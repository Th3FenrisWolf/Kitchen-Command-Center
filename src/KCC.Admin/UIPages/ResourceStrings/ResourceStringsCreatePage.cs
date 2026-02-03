using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Admin.Modules.ResourceStrings;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsListingPage),
    slug: "create",
    uiPageType: typeof(ResourceStringsCreatePage),
    name: "Create Resource String",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator
)
    : CreatePage<ResourceStringInfo, ResourceStringsEditPage>(
        formComponentMapper,
        formDataBinder,
        pageLinkGenerator
    )
{ }
