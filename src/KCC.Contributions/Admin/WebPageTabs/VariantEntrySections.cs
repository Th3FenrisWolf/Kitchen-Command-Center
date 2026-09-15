using CMS.DataEngine;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.WebPageTabs;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    parentType: typeof(VariantContributionsPage),
    slug: VariantReviewsSection.Slug,
    uiPageType: typeof(VariantReviewsSection),
    name: "Reviews",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(VariantReviewsSection),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(VariantReviewEditSection),
    name: "Review",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(VariantContributionsPage),
    slug: VariantCookNotesSection.Slug,
    uiPageType: typeof(VariantCookNotesSection),
    name: "Cook notes",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(VariantCookNotesSection),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(VariantCookNoteEditSection),
    name: "Cook note",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// A static segment above each parameterized one. Two parameterized siblings under the tab would be
/// indistinguishable in the URL, and reviews and cook notes are separate tables reached by the same
/// kind of integer id.
/// </remarks>
public sealed class VariantReviewsSection : SecondaryMenuSectionPage
{
    public const string Slug = "reviews";
}

/// <inheritdoc cref="VariantReviewsSection"/>
public sealed class VariantCookNotesSection : SecondaryMenuSectionPage
{
    public const string Slug = "cook-notes";
}

/// <summary>Carries the chosen review in the URL so the form beneath it knows which one to open.</summary>
/// <remarks>
/// The label is withheld unless the review was left against the variant in the URL. The section
/// resolves and labels its object before the form beneath it validates anything, so naming the author
/// here would put the member behind any id typed into the URL on the breadcrumb of a variant that
/// review has nothing to do with.
/// </remarks>
public sealed class VariantReviewEditSection(
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup,
    IContributionPageLookup pageLookup) : EditSectionPage<VariantReviewInfo>
{
    [PageParameter(typeof(WebPageUrlIdentifierPageModelBinder), typeof(WebPageLayout))]
    public WebPageUrlIdentifier WebPageIdentifier { get; set; } = null!;

    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        Task.FromResult(infoObject is VariantReviewInfo review && IsOnThisVariant(pageLookup, WebPageIdentifier, review.VariantGuid)
            ? ContributionTitles.Review(contentItemNameLookup.DisplayNames(), memberNameLookup.Displays(), review.VariantGuid, review.MemberGuid)
            : "Review");

    internal static bool IsOnThisVariant(
        IContributionPageLookup pageLookup,
        WebPageUrlIdentifier webPageIdentifier,
        Guid entryVariantGuid)
    {
        var identity = pageLookup.Identify(webPageIdentifier.WebPageItemID);

        return identity.Is(ContributionContentTypes.RecipeVariant)
            && entryVariantGuid == identity.ContentItemGuid;
    }
}

/// <inheritdoc cref="VariantReviewEditSection"/>
public sealed class VariantCookNoteEditSection(
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup,
    IContributionPageLookup pageLookup) : EditSectionPage<VariantCookNoteInfo>
{
    [PageParameter(typeof(WebPageUrlIdentifierPageModelBinder), typeof(WebPageLayout))]
    public WebPageUrlIdentifier WebPageIdentifier { get; set; } = null!;

    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        Task.FromResult(infoObject is VariantCookNoteInfo note
            && VariantReviewEditSection.IsOnThisVariant(pageLookup, WebPageIdentifier, note.VariantGuid)
            ? ContributionTitles.Note(contentItemNameLookup.DisplayNames(), memberNameLookup.Displays(), note.VariantGuid, note.MemberGuid)
            : "Cook note");
}
