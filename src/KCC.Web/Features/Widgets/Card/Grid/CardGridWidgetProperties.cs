using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Base;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Widgets.Card.Grid;

public class CardGridWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    [ContentItemSelectorComponent(
        CardItem.CONTENT_TYPE_NAME,
        Order = 0,
        Label = "Cards"
    )]
    public IEnumerable<ContentItemReference> Cards { get; set; }
}
