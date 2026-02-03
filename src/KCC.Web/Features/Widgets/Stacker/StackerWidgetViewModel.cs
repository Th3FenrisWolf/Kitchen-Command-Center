using System.Collections.Generic;
using KCC.Web.Features.Widgets.Base;

namespace KCC.Web.Features.Widgets.Stacker;

public class StackerWidgetViewModel : BaseWidgetViewModel
{
    public string Heading { get; set; }
    public string Body { get; set; }
    public ImageItem Image { get; set; }
    public IEnumerable<CardItem> Cards { get; set; } = [];
}
