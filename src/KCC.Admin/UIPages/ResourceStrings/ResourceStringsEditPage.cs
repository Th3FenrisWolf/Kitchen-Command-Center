using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Admin.Modules.ResourceStrings;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsSectionPage),
    slug: "edit",
    uiPageType: typeof(ResourceStringsEditPage),
    name: "Edit Resource String",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder
) : InfoEditPage<ResourceStringInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder))]
    public override int ObjectId { get; set; }
}
