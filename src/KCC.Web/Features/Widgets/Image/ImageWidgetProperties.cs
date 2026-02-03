using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Base;
using KCC.Web.Features.Widgets.Image;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterWidget(
    identifier: ImageWidgetProperties.IDENTIFIER,
    name: "Image",
    propertiesType: typeof(ImageWidgetProperties),
    customViewName: "~/Features/Widgets/Image/_ImageWidget.cshtml",
    IconClass = "icon-picture",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Image;

public class ImageWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    public const string IDENTIFIER = "KCC.Web.ImageWidget";

    [ContentItemSelectorComponent(
        ImageItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Image",
        MaximumItems = 1
    )]
    public IEnumerable<ContentItemReference> Image { get; set; }

    [TextInputComponent(Order = 10, Label = "Alt Text")]
    public string ImageAltText { get; set; }
}
