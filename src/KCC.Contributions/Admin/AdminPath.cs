namespace KCC.Contributions.Admin;

/// <remarks>
/// <c>IPageLinkGenerator</c> is inconsistent about the administration prefix and the option holding it
/// is internal to the admin assembly, so neither the presence nor the absence can be relied on: a path
/// to a page under a custom application comes back bare, while one under the Web pages application
/// already carries <c>/admin</c>. An unprefixed path used as an anchor href leaves the administration
/// and 404s against the live site; a doubled one 404s inside it.
/// </remarks>
public static class AdminPath
{
    private const string Prefix = "/admin";

    public static string ToAdminUrl(string uiTreePath)
    {
        var path = $"/{uiTreePath.TrimStart('/')}";

        return path == Prefix || path.StartsWith($"{Prefix}/", StringComparison.Ordinal)
            ? path
            : $"{Prefix}{path}";
    }
}
