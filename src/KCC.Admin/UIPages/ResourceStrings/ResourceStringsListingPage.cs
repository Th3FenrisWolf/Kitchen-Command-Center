using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using KCC.Admin.Modules.ResourceStrings;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsApplication),
    slug: "list",
    uiPageType: typeof(ResourceStringsListingPage),
    name: "Resource Strings List",
    templateName: TemplateNames.LISTING,
    order: 0
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsListingPage : ListingPage
{
    protected override string ObjectType => ResourceStringInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.HeaderActions.AddLink<ResourceStringsCreatePage>("New String");

        PageConfiguration.AddEditRowAction<ResourceStringsEditPage>();

        PageConfiguration
            .ColumnConfigurations.AddColumn(nameof(ResourceStringInfo.Key), "Key")
            .AddColumn(nameof(ResourceStringInfo.Value), "Value");

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
