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
}
