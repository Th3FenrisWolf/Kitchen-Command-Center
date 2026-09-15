using CMS.Helpers;
using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "cooked",
    uiPageType: typeof(CookedListingPage),
    name: "Cooked",
    templateName: TemplateNames.LISTING,
    order: 3
)]

namespace KCC.Contributions.Admin;

public class CookedListingPage(ContentItemNameLookup contentItemNameLookup, MemberNameLookup memberNameLookup) : ListingPage
{
    private IReadOnlyDictionary<Guid, string> contentItemNames = new Dictionary<Guid, string>();
    private IReadOnlyDictionary<Guid, MemberDisplay> members = new Dictionary<Guid, MemberDisplay>();

    protected override string ObjectType => VariantCookedInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        contentItemNames = contentItemNameLookup.DisplayNames();
        members = memberNameLookup.Displays();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantCookedInfo.RecipeGuid), "Recipe", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookedInfo.VariantGuid), "Variant", sortable: false,
                formatter: (value, _) => ContentItemNameLookup.DisplayOrDeleted(contentItemNames, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookedInfo.MemberGuid), "Member", sortable: false,
                formatter: (value, _) => MemberNameLookup.DisplayOrDeleted(members, ValidationHelper.GetGuid(value, Guid.Empty)))
            .AddColumn(nameof(VariantCookedInfo.CookedCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
