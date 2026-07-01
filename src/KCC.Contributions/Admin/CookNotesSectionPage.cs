using CMS.DataEngine;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(CookNotesListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(CookNotesSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class CookNotesSectionPage : EditSectionPage<VariantCookNoteInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject) =>
        Task.FromResult(infoObject is VariantCookNoteInfo note ? $"Note #{note.VariantCookNoteID}" : infoObject?.ToString() ?? "Note");
}
