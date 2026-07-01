using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Contributions.Admin;
using KCC.Contributions.Data;

[assembly: UIPage(
    parentType: typeof(ReviewsSectionPage),
    slug: "edit",
    uiPageType: typeof(ReviewsEditPage),
    name: "Edit Review",
    templateName: TemplateNames.EDIT,
    order: 0
)]

namespace KCC.Contributions.Admin;

public class ReviewsEditPage(IFormComponentMapper formComponentMapper, IFormDataBinder formDataBinder)
    : InfoEditPage<VariantReviewInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(ReviewsSectionPage))]
    public override int ObjectId { get; set; }
}
