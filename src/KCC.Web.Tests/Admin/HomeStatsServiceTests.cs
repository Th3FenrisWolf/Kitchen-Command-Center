using KCC.Admin.UIPages.Home;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KCC.Web.Tests.Admin;

public class HomeStatsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 29, 12, 0, 0, TimeSpan.Zero);

    private static HomeStatsService Build(Mock<IFeaturedAppStatsSource> source)
        => new(
            source.Object,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<ILogger<HomeStatsService>>(),
            () => Now);

    [Fact]
    public async Task GetStats_ReturnsFormattedCountAndRelativeTime()
    {
        var source = new Mock<IFeaturedAppStatsSource>();
        source.Setup(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(Count: 42, LastModified: Now.AddHours(-2)));
        source.Setup(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(Count: 17, LastModified: Now.AddDays(-3)));

        var service = Build(source);

        var stats = await service.GetStatsAsync(CancellationToken.None);

        Assert.Equal(new[] { new TileStat("pages", "42"), new TileStat("last edited", "2h") },
            stats.WebsiteChannel);
        Assert.Equal(new[] { new TileStat("items", "17"), new TileStat("last edited", "3d") },
            stats.ContentHub);
    }

    [Fact]
    public async Task GetStats_CachesResultsAcrossCalls()
    {
        var source = new Mock<IFeaturedAppStatsSource>();
        source.Setup(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(1, Now));
        source.Setup(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(1, Now));

        var service = Build(source);

        await service.GetStatsAsync(CancellationToken.None);
        await service.GetStatsAsync(CancellationToken.None);

        source.Verify(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
        source.Verify(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStats_ReturnsEmptyTileStatsWhenSourceThrows()
    {
        var source = new Mock<IFeaturedAppStatsSource>();
        source.Setup(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("db down"));
        source.Setup(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(5, Now));

        var service = Build(source);

        var stats = await service.GetStatsAsync(CancellationToken.None);

        Assert.Empty(stats.WebsiteChannel);
        Assert.NotEmpty(stats.ContentHub);
    }
}
