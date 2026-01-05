using System.Collections.Generic;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Card;

public class CardWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        CardItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Cards"
    )]
    public IEnumerable<ContentItemReference> Cards { get; set; }
}
