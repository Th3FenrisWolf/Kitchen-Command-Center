using System.Collections.Generic;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Card;

public class CardWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<CardItem> Cards { get; set; } = [];
}
