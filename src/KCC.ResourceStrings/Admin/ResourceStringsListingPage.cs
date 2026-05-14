using CMS.Membership;
using KCC.ResourceStrings.Admin;
using KCC.ResourceStrings.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ResourceStringsApplication),
    slug: "list",
    uiPageType: typeof(ResourceStringsListingPage),
    name: "Resource Strings List",
    templateName: TemplateNames.LISTING,
    order: 0
)]

namespace KCC.ResourceStrings.Admin;

public class ResourceStringsListingPage : ListingPage
{
    protected override string ObjectType => ResourceStringInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        _ = PageConfiguration.HeaderActions.AddLink<ResourceStringsCreatePage>("New String");

        _ = PageConfiguration.AddEditRowAction<ResourceStringsEditPage>();

        _ = PageConfiguration
            .ColumnConfigurations.AddColumn(nameof(ResourceStringInfo.ResourceStringKey), "Key", searchable: true, defaultSortDirection: SortTypeEnum.Asc)
            .AddColumn(nameof(ResourceStringInfo.ResourceStringValue), "Value", searchable: true);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
