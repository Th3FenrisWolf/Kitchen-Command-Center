using System.Collections.Generic;
using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Base;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Widgets.Stacker;

public class StackerWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [TextInputComponent(Order = 0, Label = "Heading")]
    public string Heading { get; set; }

    [RichTextEditorComponent(Order = 1, Label = "Body")]
    public string Body { get; set; }

    [ContentItemSelectorComponent(
        ImageItem.CONTENT_TYPE_NAME,
        Order = 2,
        Label = "Image"
    )]
    public IEnumerable<ContentItemReference> Image { get; set; }

    [ContentItemSelectorComponent(
        CardItem.CONTENT_TYPE_NAME,
        Order = 3,
        Label = "Cards"
    )]
    public IEnumerable<ContentItemReference> Cards { get; set; }
}
