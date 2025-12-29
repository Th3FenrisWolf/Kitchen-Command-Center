using System.Collections.Generic;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Widgets.Base;
using KitchenCommandCenter.Web.Features.Widgets.Video;

[assembly: RegisterWidget(
    identifier: VideoWidgetProperties.IDENTIFIER,
    name: "Video",
    propertiesType: typeof(VideoWidgetProperties),
    customViewName: "~/Features/Widgets/Video/_VideoWidget.cshtml",
    IconClass = "icon-brand-youtube",
    AllowCache = true
)]

namespace KitchenCommandCenter.Web.Features.Widgets.Video;

public class VideoWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    public const string IDENTIFIER = "KitchenCommandCenter.Web.VideoWidget";

    [RequiredValidationRule]
    [TextInputComponent(
        Order = 0,
        Label = "Youtube Video Code",
        ExplanationText = "The Video Code is the value after \"v=\" in a YouTube video URL"
    )]
    public string YouTubeVideoCode { get; set; }

    [RequiredValidationRule]
    [ContentItemSelectorComponent(
        ImageItem.CONTENT_TYPE_NAME,
        Order = 10,
        Label = "Thumbnail Image",
        MaximumItems = 1
    )]
    public IEnumerable<ContentItemReference> ThumbnailImage { get; set; }
}
