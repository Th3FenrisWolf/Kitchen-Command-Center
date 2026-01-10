using System.Collections.Generic;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Card.Grid;

public class CardGridWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<CardItem> Cards { get; set; } = [];
}
