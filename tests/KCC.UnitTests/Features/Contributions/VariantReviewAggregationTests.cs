using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class VariantReviewAggregationTests
{
    [Test]
    public async Task Average_ReturnsZeroCountForNoReviews()
    {
        var (avg, count) = VariantReviewInfoProvider.Aggregate(Array.Empty<int>());
        _ = await Assert.That(count).IsEqualTo(0);
        _ = await Assert.That(avg).IsEqualTo(0d);
    }

    [Test]
    public async Task Average_IsMeanOfRatings()
    {
        var (avg, count) = VariantReviewInfoProvider.Aggregate(new[] { 5, 4, 3 });
        _ = await Assert.That(count).IsEqualTo(3);
        _ = await Assert.That(avg).IsEqualTo(4d);
    }

    [Test]
    public async Task AggregateByVariant_GroupsAndAverages()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var map = VariantReviewInfoProvider.AggregateByVariant(new[] { (v1, 4), (v1, 2), (v2, 5) });
        _ = await Assert.That(map[v1].Average).IsEqualTo(3d);
        _ = await Assert.That(map[v1].Count).IsEqualTo(2);
        _ = await Assert.That(map[v2].Average).IsEqualTo(5d);
        _ = await Assert.That(map[v2].Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(0)]
    [Arguments(6)]
    [Arguments(-1)]
    public async Task ValidateRating_RejectsOutOfRange(int rating)
    {
        _ = await Assert.That(VariantReviewInfoProvider.IsValidRating(rating)).IsFalse();
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task ValidateRating_AcceptsInRange(int rating)
    {
        _ = await Assert.That(VariantReviewInfoProvider.IsValidRating(rating)).IsTrue();
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
}
