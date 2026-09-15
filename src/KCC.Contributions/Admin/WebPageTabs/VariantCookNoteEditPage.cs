using CMS.Core;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.WebPageTabs;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    parentType: typeof(VariantCookNoteEditSection),
    slug: "edit",
    uiPageType: typeof(VariantCookNoteEditPage),
    name: "Edit cook note",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.First)]

namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// The same form the Community Contributions application mounts, reached from the variant's own
/// Contributions tab instead of by leaving for another application.
/// </remarks>
public sealed class VariantCookNoteEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    ILocalizationService localizationService,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup,
    IContributionPageLookup pageLookup,
    IPageLinkGenerator pageLinkGenerator)
    : CookNoteEditPageBase(formComponentMapper, formDataBinder, localizationService, contentItemNameLookup, memberNameLookup)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(VariantCookNoteEditSection))]
    public override int ObjectId { get; set; }

    [PageParameter(typeof(StringPageModelBinder), typeof(WebPagesApplication))]
    public string ApplicationSlug { get; set; } = string.Empty;

    [PageParameter(typeof(WebPageUrlIdentifierPageModelBinder), typeof(WebPageLayout))]
    public WebPageUrlIdentifier WebPageIdentifier { get; set; } = null!;

    /// <summary>The variant page names it, so repeating it on the form only crowds the breadcrumbs.</summary>
    protected override bool ShowContentContext => false;

    /// <remarks>
    /// The variant and the cook note are bound from independent URL segments and the platform relates
    /// them in no way, so without this a cook note left against another variant opens under this
    /// variant's parameters — readable by anyone holding the ACL on this page rather than on that one.
    /// </remarks>
    public override async Task<PageValidationResult> ValidatePage()
    {
        var identity = pageLookup.Identify(WebPageIdentifier.WebPageItemID);
        var note = await GetInfoObject();

        return new PageValidationResult
        {
            IsValid = identity.Is(ContributionContentTypes.RecipeVariant)
                && note is not null
                && note.VariantGuid == identity.ContentItemGuid,
            ErrorMessageKey = ContributionTabMessages.NoteNotOnThisVariant,
        };
    }

    /// <remarks>
    /// Re-declared because the inherited command is declared against <c>Update</c>, and the Web pages
    /// application spends that name on a web page ACL rather than a grantable UI permission — so the
    /// inherited form can be satisfied by nobody but an administrator, and every save fails.
    /// </remarks>
    [PageCommand(Permission = ContributionPermissions.ManageContributions)]
    public override Task<ICommandResponse> Submit(FormSubmissionCommandArguments args) => base.Submit(args);

    /// <inheritdoc cref="Submit"/>
    [PageCommand(Permission = ContributionPermissions.ManageContributions)]
    public override Task<ICommandResponse<FormChangeResult>> Change(FormChangeCommandArguments args) =>
        base.Change(args);

    public override async Task<EditTemplateClientProperties> ConfigureTemplateProperties(
        EditTemplateClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);
        properties.Headline = "Edit cook note";
        properties.BackLink = pageLinkGenerator.GetPath<VariantContributionsPage>(new PageParameterValues
        {
            { typeof(WebPagesApplication), ApplicationSlug },
            { typeof(WebPageLayout), WebPageIdentifier.ToString() },
        });

        return properties;
    }
}
