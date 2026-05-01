using CMS;
using CMS.Core;
using KCC.Admin.UIPages.Home;

[assembly: RegisterImplementation(
    typeof(HomeApplicationsResolver),
    typeof(HomeApplicationsResolver),
    Lifestyle = Lifestyle.Singleton
)]

namespace KCC.Admin.UIPages.Home;

public sealed record AdminApplicationDescriptor(
    string Identifier,
    string Name,
    string IconName,
    string Category,
    string Url,
    int CategoryOrder = int.MaxValue);

public interface IAdminApplicationsSource
{
    Task<IReadOnlyList<AdminApplicationDescriptor>> GetAccessibleApplicationsAsync(
        CancellationToken cancellationToken
    );
}

public sealed record HomeApplicationsResult(
    IReadOnlyList<FeaturedTile> FeaturedTiles,
    IReadOnlyList<CategoryGroup> Categories
);

public sealed class HomeApplicationsResolver(IAdminApplicationsSource source)
{
    public async Task<HomeApplicationsResult> ResolveAsync(
        IReadOnlyCollection<string> featuredIdentifiers,
        CancellationToken cancellationToken)
    {
        var apps = await source.GetAccessibleApplicationsAsync(cancellationToken);

        var featured = featuredIdentifiers
            .Select(id => apps.FirstOrDefault(a => a.Identifier == id))
            .Where(a => a is not null)
            .Select(a => new FeaturedTile(
                Identifier: a.Identifier,
                Name: a.Name,
                IconName: a.IconName,
                Url: a.Url,
                Description: string.Empty,
                Stats: []))
            .ToList();

        var featuredIds = featured.Select(f => f.Identifier).ToHashSet();

        var categories = apps
            .Where(a => !featuredIds.Contains(a.Identifier))
            .GroupBy(a => a.Category)
            .Select(g => new
            {
                Group = new CategoryGroup(
                    Category: g.Key,
                    Tiles: g.Select(a => new Tile(a.Identifier, a.Name, a.IconName, a.Url)).ToList()),
                Order = g.Min(a => a.CategoryOrder),
            })
            .Where(c => c.Group.Tiles.Count > 0)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Group.Category, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Group)
            .ToList();

        return new(featured, categories);
    }
}
