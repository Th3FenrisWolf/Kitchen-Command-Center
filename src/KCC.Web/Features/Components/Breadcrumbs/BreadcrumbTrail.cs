namespace KCC.Web.Features.Components.Breadcrumbs;

public static class BreadcrumbTrail
{
    public static string Label(string breadcrumbLabel, string metadataTitle, string name) =>
        !string.IsNullOrWhiteSpace(breadcrumbLabel) ? breadcrumbLabel
        : !string.IsNullOrWhiteSpace(metadataTitle) ? metadataTitle
        : name;

    // The first crumb is the home page, which the trail shows as the home icon under its own label; the
    // last is the page itself, which is not linked.
    public static IReadOnlyList<BreadcrumbLink> Build(string homeLabel, IReadOnlyList<Crumb> rootFirst)
    {
        if (rootFirst.Count == 0)
        {
            return [];
        }

        var trail = new List<BreadcrumbLink> { new(homeLabel, rootFirst[0].Url) };
        for (var index = 1; index < rootFirst.Count; index++)
        {
            trail.Add(new(rootFirst[index].Label, index == rootFirst.Count - 1 ? string.Empty : rootFirst[index].Url));
        }

        return trail;
    }

    public sealed record Crumb(string Label, string Url);
}
