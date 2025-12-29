using System.Collections.Generic;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.SideBySide;

public class SideBySideWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<SideBySideItem> Items { get; set; } = [];
    public string Layout { get; set; } = string.Empty;
}

public class SideBySideItem
{
    public ImageItem Image { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public CtaButton Button { get; set; }
}

public class CtaButton
{
    public string ButtonText { get; set; } = string.Empty;
    public string ButtonUrl { get; set; }
    public string ButtonTarget { get; set; } = "_self";
}
