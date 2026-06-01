using CMS.ContentEngine;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Widgets.Base;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Widgets.Hero;

public class SmallHeroWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    public const string IDENTIFIER = "KCC.Web.SmallHeroWidget";

    [TextInputComponent(Order = 0, Label = "Eyebrow")]
    public string Eyebrow { get; set; }

    [RequiredValidationRule]
    [TextInputComponent(Order = 10, Label = "Title")]
    public string Title { get; set; } = WidgetConstants.ConfigHeading;

    [TextAreaComponent(Order = 20, Label = "Description")]
    public string Description { get; set; }

    [CheckBoxComponent(
        Order = 30,
        Label = "Dark Theme",
        ExplanationText = "Use the dark color palette for the hero background"
    )]
    public bool Dark { get; set; } = false;

    [ContentItemSelectorComponent(
        LinkItem.CONTENT_TYPE_NAME,
        Order = 40,
        Label = "Action Button",
        MaximumItems = 1
    )]
    public IEnumerable<ContentItemReference> ActionButton { get; set; }
}
