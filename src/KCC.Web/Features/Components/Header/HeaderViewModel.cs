using KCC.Web.Models.Common;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewModel
{
    public ImageItem Logo { get; set; }
    public IEnumerable<HeaderNavItem> MainNavItems { get; set; }
    public IEnumerable<HeaderNavItem> UtilityNavItems { get; set; }
}

public class HeaderNavItem
{
    public string DisplayText { get; set; }
    public IEnumerable<PageLink> SubLinks { get; set; }
}
