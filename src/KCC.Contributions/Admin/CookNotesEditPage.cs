using CMS.Core;
using KCC.Contributions.Admin;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(CookNotesSectionPage),
    slug: "edit",
    uiPageType: typeof(CookNotesEditPage),
    name: "Edit Note",
    templateName: TemplateNames.EDIT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class CookNotesEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    ILocalizationService localizationService,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup)
    : CookNoteEditPageBase(formComponentMapper, formDataBinder, localizationService, contentItemNameLookup, memberNameLookup)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(CookNotesSectionPage))]
    public override int ObjectId { get; set; }
}
