using KCC.Admin.UIPages.Home;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace KCC.UnitTests.Admin;

public class HomeStatsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 29, 12, 0, 0, TimeSpan.Zero);

    private static HomeStatsService Build(Mock<IFeaturedAppStatsSource> source)
        => new(
            source.Object,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<ILogger<HomeStatsService>>(),
            () => Now);

    [Test]
    public async Task GetStats_ReturnsFormattedCountAndRelativeTime()
    {
        var source = new Mock<IFeaturedAppStatsSource>();
        source.Setup(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(Count: 42, LastModified: Now.AddHours(-2)));
        source.Setup(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(Count: 17, LastModified: Now.AddDays(-3)));

        var service = Build(source);

        var stats = await service.GetStatsAsync(CancellationToken.None);

        _ = await Assert.That(stats.WebsiteChannel).IsEquivalentTo(new[] { new TileStat("pages", "42"), new TileStat("last edited", "2h") });
        _ = await Assert.That(stats.ContentHub).IsEquivalentTo(new[] { new TileStat("items", "17"), new TileStat("last edited", "3d") });
    }

    [Test]
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

    [Test]
    public async Task GetStats_ReturnsEmptyTileStatsWhenSourceThrows()
    {
        var source = new Mock<IFeaturedAppStatsSource>();
        source.Setup(s => s.GetWebsiteChannelStatsAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("db down"));
        source.Setup(s => s.GetContentHubStatsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RawStats(5, Now));

        var service = Build(source);

        var stats = await service.GetStatsAsync(CancellationToken.None);

        _ = await Assert.That(stats.WebsiteChannel.Count()).IsEqualTo(0);
        _ = await Assert.That(stats.ContentHub.Any()).IsTrue();
    }
}
