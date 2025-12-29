using System.Collections.Generic;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.SideBySide;

public class SideBySideWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        [PageBuilderPage.CONTENT_TYPE_NAME, ImageItem.CONTENT_TYPE_NAME],
        Order = 0,
        Label = "Side-by-side Items",
        ExplanationText = "Select items to be displayed in this widget. Each item will be it's own side-by-side \"block\""
    )]
    public IEnumerable<ContentItemReference> SideBySideItems { get; set; }

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 10,
        Label = "Layout",
        ExplanationText = "The layout of the first element. All subsequent items will alternate their layout",
        Options = "image-left;Image Left / Text Right\ntext-left;Text Left / Image Right"
    )]
    public string FirstElementLayout { get; set; } = "image-left";
}
