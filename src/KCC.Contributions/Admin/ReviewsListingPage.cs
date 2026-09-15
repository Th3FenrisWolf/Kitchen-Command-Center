using CMS.Helpers;
using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.Filters;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "reviews",
    uiPageType: typeof(ReviewsListingPage),
    name: "Reviews",
    templateName: TemplateNames.LISTING,
    order: 1
)]

namespace KCC.Contributions.Admin;

public class ReviewsListingPage(ContentItemNameLookup contentItemNameLookup, MemberNameLookup memberNameLookup) : ListingPage
{
    private IReadOnlyDictionary<Guid, string> contentItemNames = new Dictionary<Guid, string>();
    private IReadOnlyDictionary<Guid, MemberDisplay> members = new Dictionary<Guid, MemberDisplay>();

    protected override string ObjectType => VariantReviewInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        contentItemNames = contentItemNameLookup.DisplayNames();
        members = memberNameLookup.Displays();

        PageConfiguration.FilterConfiguration.FormModel = new ReviewsFilterModel();

        _ = PageConfiguration.AddEditRowAction<ReviewsEditPage>();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantReviewInfo.RecipeGuid), "Recipe", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantReviewInfo.VariantGuid), "Variant", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantReviewInfo.MemberGuid), "Member", sortable: false,
                formatter: (value, _) => MemberNameLookup.DisplayOrDeleted(members, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantReviewInfo.Rating), "Rating")
            .AddColumn(nameof(VariantReviewInfo.ReviewText), "Review", searchable: true)
            .AddColumn(nameof(VariantReviewInfo.ReviewCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
