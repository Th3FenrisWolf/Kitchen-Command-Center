using KCC.Contributions;

namespace KCC.UnitTests.Features.Contributions;

public class ContributionStatsTests
{
    private static readonly Guid First = Guid.NewGuid();
    private static readonly Guid Second = Guid.NewGuid();

    [Test]
    public async Task For_UnknownVariant_HasNoRatingsOrCooks()
    {
        var stats = ContributionStats.Build([], []).For(Guid.NewGuid());

        _ = await Assert.That(stats.Rating).IsEqualTo(new RatingAggregate(0d, 0));
        _ = await Assert.That(stats.CookedCount).IsEqualTo(0);
        _ = await Assert.That(stats.Distribution.Sum()).IsEqualTo(0);
    }

    [Test]
    public async Task For_AveragesTheVariantsOwnRatings()
    {
        var stats = ContributionStats.Build([(First, 5m), (First, 4m), (First, 3m), (Second, 1m)], []);

        _ = await Assert.That(stats.For(First).Rating).IsEqualTo(new RatingAggregate(4d, 3));
    }

    [Test]
    public async Task For_AveragesHalfStarsExactly()
    {
        var stats = ContributionStats.Build([(First, 4.5m), (First, 3.5m)], []);

        _ = await Assert.That(stats.For(First).Rating).IsEqualTo(new RatingAggregate(4d, 2));
    }

    [Test]
    public async Task For_BucketsTheVariantsRatings()
    {
        var stats = ContributionStats.Build([(First, 5m), (First, 4.5m), (Second, 1m)], []);

        _ = await Assert.That(string.Join(",", stats.For(First).Distribution)).IsEqualTo("0,0,0,1,1");
    }

    [Test]
    public async Task For_CountsCooksPerVariant()
    {
        var stats = ContributionStats.Build([], [First, First, Second]);

        _ = await Assert.That(stats.For(First).CookedCount).IsEqualTo(2);
        _ = await Assert.That(stats.For(Second).CookedCount).IsEqualTo(1);
    }

    [Test]
    public async Task RatingAcross_WeighsEveryReviewEqually()
    {
        // The mean of all three reviews is 4; the mean of the two variant averages would be 3.75.
        var stats = ContributionStats.Build([(First, 5m), (First, 4m), (Second, 3m)], []);

        _ = await Assert.That(stats.RatingAcross([First, Second])).IsEqualTo(new RatingAggregate(4d, 3));
    }

    [Test]
    public async Task RatingAcross_LeavesOutVariantsNotAsked()
    {
        var stats = ContributionStats.Build([(First, 5m), (Second, 1m)], []);

        _ = await Assert.That(stats.RatingAcross([First])).IsEqualTo(new RatingAggregate(5d, 1));
    }

    [Test]
    public async Task RatingAcross_CountsARepeatedKeyOnce()
    {
        var stats = ContributionStats.Build([(First, 5m)], []);

        _ = await Assert.That(stats.RatingAcross([First, First]).Count).IsEqualTo(1);
    }

    [Test]
    public async Task CookedAcross_SumsTheVariantsAsked()
    {
        var stats = ContributionStats.Build([], [First, Second, Second, Guid.NewGuid()]);

        _ = await Assert.That(stats.CookedAcross([First, Second])).IsEqualTo(3);
    }
}
