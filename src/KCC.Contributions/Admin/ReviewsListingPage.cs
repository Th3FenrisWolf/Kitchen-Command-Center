using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "reviews",
    uiPageType: typeof(ReviewsListingPage),
    name: "Reviews",
    templateName: TemplateNames.LISTING,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class ReviewsListingPage : ListingPage
{
    protected override string ObjectType => VariantReviewInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration.AddEditRowAction<ReviewsEditPage>();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantReviewInfo.VariantGuid), "Variant", searchable: true)
            .AddColumn(nameof(VariantReviewInfo.MemberGuid), "Member", searchable: true)
            .AddColumn(nameof(VariantReviewInfo.Rating), "Rating", defaultSortDirection: SortTypeEnum.Desc)
            .AddColumn(nameof(VariantReviewInfo.ReviewText), "Review")
            .AddColumn(nameof(VariantReviewInfo.ReviewCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
