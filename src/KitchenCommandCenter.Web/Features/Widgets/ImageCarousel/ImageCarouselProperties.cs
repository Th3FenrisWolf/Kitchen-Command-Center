using System.Collections.Generic;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.ImageCarousel;

public class ImageCarouselProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        ImageItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Carousel Items",
        ExplanationText = "Select the images to be displayed in this widget"
    )]
    public IEnumerable<ContentItemReference> CarouselItems { get; set; }

    [CheckBoxComponent(
        Order = 10,
        Label = "Crop Images to Fit?",
        ExplanationText = "If images with transparency are being used, this should be left unchecked."
    )]
    public bool CropImages { get; set; } = false;

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 30,
        Label = "Scroll Behavior",
        Options = "Manual\nContinuous\nAutomatic"
    )]
    public string ScrollBehavior { get; set; } = "Manual";

    [NumberInputComponent(
        Order = 40,
        Label = "Items to Display",
        ExplanationText = "The number of carousel items to be displayed when viewing the widget on a desktop. If this number matches or exceeds the number of carousel items, navigation is disabled"
    )]
    public int DisplayCount { get; set; } = 5;
}
