using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class VariantReviewAggregationTests
{
    [Test]
    public async Task Average_ReturnsZeroCountForNoReviews()
    {
        var (avg, count) = VariantReviewInfoProvider.Aggregate(Array.Empty<decimal>());
        _ = await Assert.That(count).IsEqualTo(0);
        _ = await Assert.That(avg).IsEqualTo(0d);
    }

    [Test]
    public async Task Average_IsMeanOfRatings()
    {
        var (avg, count) = VariantReviewInfoProvider.Aggregate(new[] { 5m, 4m, 3m });
        _ = await Assert.That(count).IsEqualTo(3);
        _ = await Assert.That(avg).IsEqualTo(4d);
    }

    [Test]
    public async Task Average_HandlesHalfStars()
    {
        var (avg, count) = VariantReviewInfoProvider.Aggregate(new[] { 4.5m, 3.5m });
        _ = await Assert.That(count).IsEqualTo(2);
        _ = await Assert.That(avg).IsEqualTo(4d);
    }

    [Test]
    public async Task AggregateByVariant_GroupsAndAverages()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var map = VariantReviewInfoProvider.AggregateByVariant(new[] { (v1, 4m), (v1, 2m), (v2, 5m) });
        _ = await Assert.That(map[v1].Average).IsEqualTo(3d);
        _ = await Assert.That(map[v1].Count).IsEqualTo(2);
        _ = await Assert.That(map[v2].Average).IsEqualTo(5d);
        _ = await Assert.That(map[v2].Count).IsEqualTo(1);
    }

    // decimal cannot be an attribute argument, so pass doubles and cast in-body.
    [Test]
    [Arguments(0.0)]
    [Arguments(5.5)]
    [Arguments(3.7)]
    [Arguments(-1.0)]
    public async Task ValidateRating_RejectsInvalid(double rating)
    {
        _ = await Assert.That(VariantReviewInfoProvider.IsValidRating((decimal)rating)).IsFalse();
    }

    [Test]
    [Arguments(0.5)]
    [Arguments(2.5)]
    [Arguments(5.0)]
    public async Task ValidateRating_AcceptsHalfSteps(double rating)
    {
        _ = await Assert.That(VariantReviewInfoProvider.IsValidRating((decimal)rating)).IsTrue();
    }

    [Test]
    public async Task CanModify_TrueOnlyForAuthor()
    {
        var member = Guid.NewGuid();
        var other = Guid.NewGuid();
        _ = await Assert.That(VariantReviewInfoProvider.CanModify(member, member)).IsTrue();
        _ = await Assert.That(VariantReviewInfoProvider.CanModify(other, member)).IsFalse();
        _ = await Assert.That(VariantReviewInfoProvider.CanModify(null, member)).IsFalse();
    }

    [Test]
    public async Task Distribution_CountsWholeStarsByBucket()
    {
        // index 0 = 1★ … index 4 = 5★
        var dist = VariantReviewInfoProvider.Distribution(new[] { 5m, 5m, 4m, 3m, 1m });
        _ = await Assert.That(dist[4]).IsEqualTo(2);
        _ = await Assert.That(dist[3]).IsEqualTo(1);
        _ = await Assert.That(dist[2]).IsEqualTo(1);
        _ = await Assert.That(dist[1]).IsEqualTo(0);
        _ = await Assert.That(dist[0]).IsEqualTo(1);
    }

    [Test]
    public async Task Distribution_RoundsHalfStarsDown()
    {
        // 4.5 → 4★, 3.5 → 3★, 1.5 → 1★ (a half is "not yet" the higher star)
        var dist = VariantReviewInfoProvider.Distribution(new[] { 4.5m, 3.5m, 1.5m });
        _ = await Assert.That(dist[4]).IsEqualTo(0);
        _ = await Assert.That(dist[3]).IsEqualTo(1);
        _ = await Assert.That(dist[2]).IsEqualTo(1);
        _ = await Assert.That(dist[0]).IsEqualTo(1);
    }

    [Test]
    public async Task Distribution_ClampsLoneHalfStarIntoOneStar()
    {
        // 0.5 has no 0★ bucket, so it lands in 1★.
        var dist = VariantReviewInfoProvider.Distribution(new[] { 0.5m });
        _ = await Assert.That(dist[0]).IsEqualTo(1);
    }

    [Test]
    public async Task Distribution_EmptyForNoRatings()
    {
        var dist = VariantReviewInfoProvider.Distribution(Array.Empty<decimal>());
        _ = await Assert.That(dist.Sum()).IsEqualTo(0);
    }
}
