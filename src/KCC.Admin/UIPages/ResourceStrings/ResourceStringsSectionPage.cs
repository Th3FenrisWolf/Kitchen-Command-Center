using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using KCC.Admin.Modules.ResourceStrings;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(ResourceStringsSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsSectionPage : EditSectionPage<ResourceStringInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        return infoObject is not ResourceStringInfo resourceString
            ? await base.GetObjectDisplayName(infoObject)
            : resourceString.Key;
    }
}
