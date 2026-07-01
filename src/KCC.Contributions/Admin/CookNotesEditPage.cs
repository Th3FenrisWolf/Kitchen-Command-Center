using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;

[assembly: UIPage(
    parentType: typeof(CookNotesSectionPage),
    slug: "edit",
    uiPageType: typeof(CookNotesEditPage),
    name: "Edit Note",
    templateName: TemplateNames.EDIT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class CookNotesEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<VariantCookNoteInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(CookNotesSectionPage))]
    public override int ObjectId { get; set; }
}
