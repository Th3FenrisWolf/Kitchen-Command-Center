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
    slug: RecipeContributionsPage.Slug,
    uiPageType: typeof(RecipeContributionsPage),
    name: RecipeContributionsPage.Caption,
    templateName: $"@{ContributionsModule.OrgName}/{ContributionsModule.ProjectName}/RecipeContributions",
    order: 400,
    Icon = Icons.StarEmpty)]

namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// Answers what the community made of this recipe and which of its variants the feedback landed on.
/// Everything the tab shows is server-rendered: the variant rows link on to each variant's own tab
/// rather than expanding in place, because those pages are already in the content tree beside this one.
///
/// Derived from <see cref="WebPageBase{TClientProperties}"/> so the page inherits the READ ACL check on
/// the recipe. Member names are on this screen by way of the variant tabs it links to, and
/// <c>PageInvoker</c> activates only the routed node, so the layout's own check does not run here.
/// </remarks>
[UIEvaluatePermission(ContributionPermissions.ViewContributions)]
public sealed class RecipeContributionsPage(
    IAuthenticatedUserAccessor userAccessor,
    IWebPageManagerFactory webPageManagerFactory,
    IPageLinkGenerator linkGenerator,
    IContributionPageLookup pageLookup,
    ContributionTabService tabService,
    ContentItemNameLookup contentItemNameLookup
) : WebPageBase<RecipeContributionsClientProperties>(userAccessor, webPageManagerFactory, linkGenerator)
{
    public const string Slug = "recipe-contributions";

    public const string Caption = "Contributions";

    /// <remarks>
    /// Held separately because the base captures the same instance in a field this assembly cannot
    /// reach, so using the constructor parameter directly would capture it twice.
    /// </remarks>
    private readonly IPageLinkGenerator links = linkGenerator;

    public override Task<PageValidationResult> ValidatePage() => Task.FromResult(new PageValidationResult
    {
        IsValid = pageLookup.Identify(WebPageIdentifier.WebPageItemID).Is(ContributionContentTypes.Recipe),
        ErrorMessageKey = ContributionTabMessages.NotARecipe,
    });

    public override async Task<RecipeContributionsClientProperties> ConfigureTemplateProperties(
        RecipeContributionsClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);

        var identity = pageLookup.Identify(WebPageIdentifier.WebPageItemID);
        var rollup = tabService.GetRecipeRollup(identity.ContentItemGuid);
        var variantPages = pageLookup.GetVariantPages(WebPageIdentifier.WebPageItemID);
        var names = contentItemNameLookup.DisplayNames();

        properties.Totals = rollup.Totals;
        properties.Variants =
        [
            ..variantPages.Select(variant =>
            {
                var totals = rollup.ByVariant.GetValueOrDefault(variant.ContentItemGuid, ContributionTotals.Empty);

                return new VariantContributionRow(
                    ContentItemNameLookup.DisplayOrDeleted(names, variant.ContentItemGuid),
                    totals.AverageRating,
                    totals.ReviewCount,
                    totals.NoteCount,
                    totals.CookedCount,
                    totals.LastActivity,
                    ContributionsUrlFor(variant.WebPageItemId));
            })
        ];
        properties.Orphaned = ContributionTabService.OrphanedTotals(
            rollup,
            [..variantPages.Select(variant => variant.ContentItemGuid)]);

        return properties;
    }

    private string ContributionsUrlFor(int variantWebPageItemId) => AdminPath.ToAdminUrl(
        links.GetPath<VariantContributionsPage>(new PageParameterValues
        {
            { typeof(WebPagesApplication), ApplicationIdentifier.Slug },
            {
                typeof(WebPageLayout),
                new WebPageUrlIdentifier(WebPageIdentifier.LanguageName, variantWebPageItemId).ToString()
            },
        }));
}
