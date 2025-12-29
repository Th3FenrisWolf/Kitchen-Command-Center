using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using KitchenCommandCenter.Admin.Modules.ResourceStrings;

[assembly: UIApplication(
    identifier: ResourceStringsApplication.IDENTIFIER,
    type: typeof(ResourceStringsApplication),
    slug: "resource-strings",
    name: "Resource Strings",
    category: BaseApplicationCategories.CONFIGURATION,
    icon: Icons.Translate,
    templateName: TemplateNames.SECTION_LAYOUT
)]

namespace KitchenCommandCenter.Admin.Modules.ResourceStrings;

public class ResourceStringsApplication : ApplicationPage
{
    public const string IDENTIFIER = "KitchenCommandCenter.Admin.ResourceStringsApplication";
}
