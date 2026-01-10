using System.Collections.Generic;
using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Base;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Widgets.Card;

public class CardWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        CardItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Card",
        MaximumItems = 1
    )]
    public IEnumerable<ContentItemReference> Card { get; set; }
}
