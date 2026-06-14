using KCC.Admin.UIPages.Home;
using Moq;

namespace KCC.UnitTests.Admin;

public class HomeApplicationsResolverTests
{
    private static AdminApplicationDescriptor App(
        string identifier,
        string category,
        string name = "App",
        string iconName = "icon",
        string url = "url")
        => new(identifier, name, iconName, category, url);

    [Test]
    public async Task Resolve_GroupsAccessibleAppsByCategory()
    {
        var source = new Mock<IAdminApplicationsSource>();
        source.Setup(s => s.GetAccessibleApplicationsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  App("a", "Configuration"),
                  App("b", "Configuration"),
                  App("c", "Development"),
              });

        var resolver = new HomeApplicationsResolver(source.Object);

        var result = await resolver.ResolveAsync(featuredIdentifiers: Array.Empty<string>(), CancellationToken.None);

        _ = await Assert.That(result.FeaturedTiles.Count()).IsEqualTo(0);
        _ = await Assert.That(result.Categories.Count).IsEqualTo(2);
        _ = await Assert.That(result.Categories[0].Category).IsEqualTo("Configuration");
        _ = await Assert.That(result.Categories[0].Tiles.Count).IsEqualTo(2);
        _ = await Assert.That(result.Categories[1].Category).IsEqualTo("Development");
        _ = await Assert.That(result.Categories[1].Tiles.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_PullsFeaturedAppsOutOfCategoryGroups()
    {
        var source = new Mock<IAdminApplicationsSource>();
        source.Setup(s => s.GetAccessibleApplicationsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  App("Kentico.WebsiteChannels", "Content management"),
                  App("Kentico.ContentHub", "Content management"),
                  App("other", "Content management"),
              });

        var resolver = new HomeApplicationsResolver(source.Object);

        var result = await resolver.ResolveAsync(
            new[] { "Kentico.WebsiteChannels", "Kentico.ContentHub" },
            CancellationToken.None);

        _ = await Assert.That(result.FeaturedTiles.Count).IsEqualTo(2);
        _ = await Assert.That(result.Categories.Count()).IsEqualTo(1);
        _ = await Assert.That(result.Categories[0].Tiles.Count()).IsEqualTo(1);
        _ = await Assert.That(result.Categories[0].Tiles[0].Identifier).IsEqualTo("other");
    }

    [Test]
    public async Task Resolve_HidesEmptyCategories()
    {
        var source = new Mock<IAdminApplicationsSource>();
        source.Setup(s => s.GetAccessibleApplicationsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  App("only-featured", "Content management"),
              });

        var resolver = new HomeApplicationsResolver(source.Object);

        var result = await resolver.ResolveAsync(
            new[] { "only-featured" },
            CancellationToken.None);

        _ = await Assert.That(result.FeaturedTiles.Count()).IsEqualTo(1);
        _ = await Assert.That(result.Categories.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_OmitsFeaturedTileWhenUserLacksAccess()
    {
        var source = new Mock<IAdminApplicationsSource>();
        source.Setup(s => s.GetAccessibleApplicationsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  App("a", "Configuration"),
              });

        var resolver = new HomeApplicationsResolver(source.Object);

        var result = await resolver.ResolveAsync(
            new[] { "Kentico.WebsiteChannels", "Kentico.ContentHub" },
            CancellationToken.None);

        _ = await Assert.That(result.FeaturedTiles.Count()).IsEqualTo(0);
        _ = await Assert.That(result.Categories.Count()).IsEqualTo(1);
    }
}
