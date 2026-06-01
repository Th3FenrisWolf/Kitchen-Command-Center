using KCC.Web.Features.Models.Common;

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

    /// <summary>
    /// Gets or sets the direct-link URL for a flat (NavLink-backed) entry. Null when
    /// this entry is backed by a NavItem with <see cref="SubLinks"/>.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the link target (e.g. <c>_self</c>, <c>_blank</c>) paired with
    /// <see cref="Url"/>. Null when this entry is a dropdown.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the dropdown children for a NavItem-backed entry. Null when this
    /// entry is a flat link (see <see cref="Url"/>).
    /// </summary>
    public IEnumerable<PageLink> SubLinks { get; set; }
}
