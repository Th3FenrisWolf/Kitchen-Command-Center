using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using KCC.Admin.Modules.ResourceStrings;

[assembly: UIApplication(
    identifier: ResourceStringsApplication.IDENTIFIER,
    type: typeof(ResourceStringsApplication),
    slug: "resource-strings",
    name: "Resource Strings",
    category: BaseApplicationCategories.CONFIGURATION,
    icon: Icons.Translate,
    templateName: TemplateNames.SECTION_LAYOUT
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsApplication : ApplicationPage
{
    public const string IDENTIFIER = "KCC.Admin.ResourceStringsApplication";
}
