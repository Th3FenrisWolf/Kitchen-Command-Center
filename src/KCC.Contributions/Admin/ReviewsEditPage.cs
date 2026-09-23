using CMS.Core;
using KCC.Contributions.Admin;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(ReviewsSectionPage),
    slug: "edit",
    uiPageType: typeof(ReviewsEditPage),
    name: "Edit Review",
    templateName: TemplateNames.EDIT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class ReviewsEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    ILocalizationService localizationService,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup)
    : ReviewEditPageBase(formComponentMapper, formDataBinder, localizationService, contentItemNameLookup, memberNameLookup)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(ReviewsSectionPage))]
    public override int ObjectId { get; set; }
}
