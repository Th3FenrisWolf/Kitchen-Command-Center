namespace KCC.Web.Features.Components.Header;

public sealed record HeaderNavigation(IReadOnlyList<NavEntry> Main, IReadOnlyList<NavEntry> Utility)
{
    public static HeaderNavigation Empty { get; } = new([], []);
}

public sealed record NavEntry(string DisplayText, string ShowWhen, NavTarget Link, IReadOnlyList<NavTarget> Links);

public sealed record NavTarget(string DisplayText, string Url, string Target);
