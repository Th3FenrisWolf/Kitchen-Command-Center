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
    order: 2
)]

namespace KCC.Contributions.Admin;

public class CookedListingPage : ListingPage
{
    protected override string ObjectType => VariantCookedInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration
            .ColumnConfigurations
            .AddColumn(nameof(VariantCookedInfo.VariantGuid), "Variant", searchable: true)
            .AddColumn(nameof(VariantCookedInfo.MemberGuid), "Member", searchable: true)
            .AddColumn(nameof(VariantCookedInfo.CookedCreated), "Date", defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
