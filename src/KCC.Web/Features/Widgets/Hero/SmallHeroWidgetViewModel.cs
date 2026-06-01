using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Widgets.Base;

namespace KCC.Web.Features.Widgets.Hero;

public class SmallHeroWidgetViewModel : BaseWidgetViewModel
{
    public string Eyebrow { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Dark { get; set; }
    public PageLink ActionLink { get; set; }
}
