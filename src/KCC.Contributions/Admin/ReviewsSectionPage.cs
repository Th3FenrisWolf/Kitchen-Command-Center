using CMS.DataEngine;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ReviewsListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(ReviewsSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class ReviewsSectionPage : EditSectionPage<VariantReviewInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        Task.FromResult(infoObject is VariantReviewInfo review ? $"Review #{review.VariantReviewID}" : infoObject?.ToString() ?? "Review");
}
