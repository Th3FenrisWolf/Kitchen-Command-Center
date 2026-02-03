using KCC.Web.Features.Widgets.Base;

namespace KCC.Web.Features.Widgets.Card.Grid;

public class CardGridWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<CardItem> Cards { get; set; } = [];
}
