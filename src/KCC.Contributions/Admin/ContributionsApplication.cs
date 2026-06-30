using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using KCC.Contributions.Admin;

[assembly: UIApplication(
    identifier: ContributionsApplication.IDENTIFIER,
    type: typeof(ContributionsApplication),
    slug: "community-contributions",
    name: "Community Contributions",
    category: BaseApplicationCategories.CONFIGURATION,
    icon: Icons.StarFull,
    templateName: TemplateNames.SECTION_LAYOUT
)]

namespace KCC.Contributions.Admin;

public class ContributionsApplication : ApplicationPage
{
    public const string IDENTIFIER = "KCC.Admin.ContributionsApplication";
}
