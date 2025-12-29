using System.Collections.Generic;
using System.Linq;
using KitchenCommandCenter.Web.Features.Widgets.Base;

namespace KitchenCommandCenter.Web.Features.Widgets.Accordion;

public class AccordionWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<AccordionItem> Items { get; set; } = Enumerable.Empty<AccordionItem>();
}

public class AccordionItem
{
    public string Heading { get; set; }
    public string Body { get; set; }
    public bool IsDefaultOpen { get; set; }
}
