using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewModel
{
    public string LogoAlt { get; set; }

    // Resolved server-side because the header renders on every page, while each page controller assembles its
    // own resource strings.
    public string SwitchToLightLabel { get; set; }

    public string SwitchToDarkLabel { get; set; }

    public IEnumerable<HeaderNavItem> MainNavItems { get; set; }

    public IEnumerable<HeaderNavItem> UtilityNavItems { get; set; }

    public NavModel Nav { get; set; }
}

public class HeaderNavItem
{
    public string DisplayText { get; set; }

    public string Url { get; set; }

    public string Target { get; set; }

    public IEnumerable<PageLink> SubLinks { get; set; }
}
