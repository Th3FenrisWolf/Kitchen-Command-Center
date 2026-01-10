using System.Collections.Generic;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Stacker;

public class StackerWidgetViewModel : BaseWidgetViewModel
{
    public string Heading { get; set; }
    public string Body { get; set; }
    public ImageItem Image { get; set; }
    public IEnumerable<CardItem> Cards { get; set; } = [];
}
