using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using KCC.Admin.UIPages.Home;

[assembly: UIApplication(
    identifier: HomeApplication.IDENTIFIER,
    type: typeof(HomeApplication),
    slug: "home",
    name: "Home",
    category: BaseApplicationCategories.DASHBOARD,
    icon: Icons.Home,
    templateName: "@kcc/admin/Home"
)]

namespace KCC.Admin.UIPages.Home;

public class HomeApplication(
    IAdminApplicationsSource applicationsSource,
    HomeApplicationsResolver applicationsResolver,
    HomeStatsService statsService
) : Page<HomeApplicationProperties>
{
    public const string IDENTIFIER = "KCC.Admin.HomeApplication";

    private const string ContentHubIdentifier = ContentHubApplication.IDENTIFIER;

    public override async Task<HomeApplicationProperties> ConfigureTemplateProperties(HomeApplicationProperties properties)
    {
        var allApps = await applicationsSource.GetAccessibleApplicationsAsync(CancellationToken.None);

        var firstChannel = allApps.FirstOrDefault(a => a.Identifier.StartsWith(
            KenticoAdminApplicationsSource.DynamicChannelIdentifierPrefix,
            StringComparison.Ordinal));

        var featuredIdentifiers = new List<string>();
        if (firstChannel is not null)
        {
            featuredIdentifiers.Add(firstChannel.Identifier);
        }

        featuredIdentifiers.Add(ContentHubIdentifier);

        var apps = await applicationsResolver.ResolveAsync(featuredIdentifiers, CancellationToken.None);

        var stats = await statsService.GetStatsAsync(CancellationToken.None);

        properties.FeaturedTiles = [..apps.FeaturedTiles.Select(tile => tile with
        {
            Description = DescriptionFor(tile.Identifier),
            Stats = StatsFor(tile.Identifier, stats),
        })];

        properties.Categories = apps.Categories;

        return properties;
    }

    private static string DescriptionFor(string identifier)
    {
        if (identifier.Equals(ContentHubIdentifier, StringComparison.Ordinal))
        {
            return "Browse and edit reusable content";
        }

        if (identifier.StartsWith(KenticoAdminApplicationsSource.DynamicChannelIdentifierPrefix, StringComparison.Ordinal))
        {
            return "Manage live website pages and structure";
        }

        return string.Empty;
    }

    private static IReadOnlyList<TileStat> StatsFor(string identifier, FeaturedStats stats)
    {
        if (identifier.Equals(ContentHubIdentifier, StringComparison.Ordinal))
        {
            return stats.ContentHub;
        }

        if (identifier.StartsWith(KenticoAdminApplicationsSource.DynamicChannelIdentifierPrefix, StringComparison.Ordinal))
        {
            return stats.WebsiteChannel;
        }

        return [];
    }
}
