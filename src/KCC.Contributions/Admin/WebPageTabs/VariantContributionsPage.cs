using CMS.Membership;
using CMS.Websites;
using KCC.Contributions;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.WebPageTabs;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    parentType: typeof(WebPageLayout),
    slug: VariantContributionsPage.Slug,
    uiPageType: typeof(VariantContributionsPage),
    name: VariantContributionsPage.Caption,
    templateName: $"@{ContributionsModule.OrgName}/{ContributionsModule.ProjectName}/VariantContributions",
    order: 400,
    Icon = Icons.StarEmpty)]

namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// The moderation surface: every review and cook note left against this variant, in full, with the
/// author and the date, and a delete for each.
///
/// Derived from <see cref="WebPageBase{TClientProperties}"/> so the page inherits the READ ACL check on
/// the variant. Member names and free text written by the public are on this screen, and
/// <c>PageInvoker</c> activates only the routed node, so the layout's own check does not run for a
/// child tab.
/// </remarks>
[UIEvaluatePermission(ContributionPermissions.ViewContributions)]
public sealed class VariantContributionsPage(
    IAuthenticatedUserAccessor userAccessor,
    IWebPageManagerFactory webPageManagerFactory,
    IPageLinkGenerator linkGenerator,
    IContributionPageLookup pageLookup,
    ContributionTabService tabService
) : WebPageBase<VariantContributionsClientProperties>(userAccessor, webPageManagerFactory, linkGenerator)
{
    public const string Slug = "variant-contributions";

    public const string Caption = "Contributions";

    /// <remarks>
    /// Held separately because the base captures the same instance in a field this assembly cannot
    /// reach, so using the constructor parameter directly would capture it twice.
    /// </remarks>
    private readonly IPageLinkGenerator links = linkGenerator;

    private ContributionPageIdentity Identity => pageLookup.Identify(WebPageIdentifier.WebPageItemID);

    public override Task<PageValidationResult> ValidatePage() => Task.FromResult(new PageValidationResult
    {
        IsValid = Identity.Is(ContributionContentTypes.RecipeVariant),
        ErrorMessageKey = ContributionTabMessages.NotAVariant,
    });

    public override async Task<VariantContributionsClientProperties> ConfigureTemplateProperties(
        VariantContributionsClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);

        var identity = Identity;

        properties.Totals = tabService.GetVariantTotals(identity.ContentItemGuid);
        properties.RatingDistribution = tabService.GetRatingDistribution(identity.ContentItemGuid);
        properties.RecipeContributionsUrl = RecipeContributionsUrlFor(identity.ParentWebPageItemId);
        properties.EntryPageSize = ContributionTabService.EntryPageSize;
        properties.CanDelete = await HasAclPermissionAsync(WebPageAclPermissions.DELETE);
        properties.Reviews = tabService.GetReviews(identity.ContentItemGuid, 0, ReviewEditUrlFor);
        properties.Notes = tabService.GetCookNotes(identity.ContentItemGuid, 0, CookNoteEditUrlFor);

        return properties;
    }

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<ContributionEntryPage>> GetReviews(ContributionEntriesArgs args) =>
        Task.FromResult(ResponseFrom(tabService.GetReviews(Identity.ContentItemGuid, args.Page, ReviewEditUrlFor)));

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<ContributionEntryPage>> GetCookNotes(ContributionEntriesArgs args) =>
        Task.FromResult(ResponseFrom(tabService.GetCookNotes(Identity.ContentItemGuid, args.Page, CookNoteEditUrlFor)));

    /// <remarks>
    /// Declared against VIEW and checking DELETE on the page itself, because a
    /// <see cref="PageCommandAttribute"/> permission is evaluated against the application while the
    /// authority over a contribution is the ACL on the page it was left against. This is the idiom the
    /// platform's own web page commands use.
    /// </remarks>
    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<ContributionMutationResult>> DeleteReview(ContributionEntryArgs args)
    {
        await CheckAclPermission(WebPageIdentifier.WebPageItemID, WebPageAclPermissions.DELETE);

        var variantGuid = Identity.ContentItemGuid;
        var deleted = tabService.DeleteReview(args.Id, variantGuid);
        var response = ResponseFrom(RefreshedFrom(variantGuid, tabService.GetReviews(variantGuid, 0, ReviewEditUrlFor)));

        return deleted
            ? response.AddSuccessMessage("Review deleted.")
            : response.AddErrorMessage("That review is no longer available.");
    }

    /// <inheritdoc cref="DeleteReview"/>
    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<ContributionMutationResult>> DeleteCookNote(ContributionEntryArgs args)
    {
        await CheckAclPermission(WebPageIdentifier.WebPageItemID, WebPageAclPermissions.DELETE);

        var variantGuid = Identity.ContentItemGuid;
        var deleted = tabService.DeleteCookNote(args.Id, variantGuid);
        var response = ResponseFrom(RefreshedFrom(variantGuid, tabService.GetCookNotes(variantGuid, 0, CookNoteEditUrlFor)));

        return deleted
            ? response.AddSuccessMessage("Cook note deleted.")
            : response.AddErrorMessage("That cook note is no longer available.");
    }

    private ContributionMutationResult RefreshedFrom(Guid variantGuid, ContributionEntryPage entries) => new(
        tabService.GetVariantTotals(variantGuid),
        tabService.GetRatingDistribution(variantGuid),
        entries);

    /// <remarks>
    /// The evaluator behind the platform's ACL check is internal, so a throwing check is the only
    /// exposed way to ask the question.
    /// </remarks>
    private async Task<bool> HasAclPermissionAsync(string permission)
    {
        try
        {
            await CheckAclPermission(WebPageIdentifier.WebPageItemID, permission);

            return true;
        }
        catch (ForbiddenAccessException)
        {
            return false;
        }
    }

    private string RecipeContributionsUrlFor(int recipeWebPageItemId) => recipeWebPageItemId == 0
        ? string.Empty
        : AdminPath.ToAdminUrl(links.GetPath<RecipeContributionsPage>(new PageParameterValues
        {
            { typeof(WebPagesApplication), ApplicationIdentifier.Slug },
            {
                typeof(WebPageLayout),
                new WebPageUrlIdentifier(WebPageIdentifier.LanguageName, recipeWebPageItemId).ToString()
            },
        }));

    /// <remarks>
    /// The edit forms are mounted beneath this tab rather than linked to in the Community
    /// Contributions application, so moderating a variant never leaves the page being moderated.
    /// </remarks>
    private string ReviewEditUrlFor(int reviewId) => AdminPath.ToAdminUrl(
        links.GetPath<VariantReviewEditPage>(EntryParameters(typeof(VariantReviewEditSection), reviewId)));

    /// <inheritdoc cref="ReviewEditUrlFor"/>
    private string CookNoteEditUrlFor(int noteId) => AdminPath.ToAdminUrl(
        links.GetPath<VariantCookNoteEditPage>(EntryParameters(typeof(VariantCookNoteEditSection), noteId)));

    private PageParameterValues EntryParameters(Type sectionType, int entryId) => new()
    {
        { typeof(WebPagesApplication), ApplicationIdentifier.Slug },
        { typeof(WebPageLayout), WebPageIdentifier.ToString() },
        { sectionType, entryId },
    };
}
