using KCC.Admin.UIPages.Home;
using Moq;
using Xunit;

namespace KCC.Web.Tests.Admin;

public class HomeApplicationsResolverTests
{
    private static AdminApplicationDescriptor App(
        string identifier,
        string category,
        string name = "App",
        string iconName = "icon",
        string url = "url")
        => new(identifier, name, iconName, category, url);

    [Fact]
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

        Assert.Empty(result.FeaturedTiles);
        Assert.Equal(2, result.Categories.Count);
        Assert.Equal("Configuration", result.Categories[0].Category);
        Assert.Equal(2, result.Categories[0].Tiles.Count);
        Assert.Equal("Development", result.Categories[1].Category);
        Assert.Single(result.Categories[1].Tiles);
    }

    [Fact]
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

        Assert.Equal(2, result.FeaturedTiles.Count);
        Assert.Single(result.Categories);
        Assert.Single(result.Categories[0].Tiles);
        Assert.Equal("other", result.Categories[0].Tiles[0].Identifier);
    }

    [Fact]
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

        Assert.Single(result.FeaturedTiles);
        Assert.Empty(result.Categories);
    }

    [Fact]
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

        Assert.Empty(result.FeaturedTiles);
        Assert.Single(result.Categories);
    }
}
