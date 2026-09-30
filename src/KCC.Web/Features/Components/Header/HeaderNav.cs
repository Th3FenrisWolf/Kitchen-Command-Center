using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Components.Header;

public static class HeaderNav
{
    public const string Always = "Always";
    public const string SignedIn = "Signed in";
    public const string SignedOut = "Signed out";

    public static IEnumerable<HeaderNavItem> Visible(IEnumerable<NavEntry> entries, bool isSignedIn) =>
        entries.Where(entry => IsVisible(entry.ShowWhen, isSignedIn) && LeadsSomewhere(entry)).Select(ToHeaderNavItem);

    private static bool IsVisible(string showWhen, bool isSignedIn) => showWhen switch
    {
        SignedIn => isSignedIn,
        SignedOut => !isSignedIn,
        _ => true,
    };

    private static bool LeadsSomewhere(NavEntry entry) => entry.Link is not null || entry.Links is { Count: > 0 };

    private static HeaderNavItem ToHeaderNavItem(NavEntry entry) => entry.Link is { } link
        ? new HeaderNavItem { DisplayText = entry.DisplayText, Url = link.Url, Target = link.Target }
        : new HeaderNavItem
        {
            DisplayText = entry.DisplayText,
            SubLinks = entry.Links.Select(target => new PageLink { DisplayText = target.DisplayText, Url = target.Url, Target = target.Target }).ToList(),
        };
}
