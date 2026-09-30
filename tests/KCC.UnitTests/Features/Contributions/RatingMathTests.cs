using KCC.Contributions;
using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class RatingMathTests
{
    // decimal cannot be an attribute argument, so the cases arrive as doubles.
    [Test]
    [Arguments(0.0)]
    [Arguments(5.5)]
    [Arguments(3.7)]
    [Arguments(-1.0)]
    public async Task IsValidRating_RejectsOffStepAndOutOfRange(double rating)
    {
        _ = await Assert.That(RatingMath.IsValidRating((decimal)rating)).IsFalse();
    }

    [Test]
    [Arguments(0.5)]
    [Arguments(2.5)]
    [Arguments(5.0)]
    public async Task IsValidRating_AcceptsHalfSteps(double rating)
    {
        _ = await Assert.That(RatingMath.IsValidRating((decimal)rating)).IsTrue();
    }

    [Test]
    public async Task ClampText_TrimsAndEmptiesBlanks()
    {
        _ = await Assert.That(RatingMath.ClampText("  tasty  ")).IsEqualTo("tasty");
        _ = await Assert.That(RatingMath.ClampText("   ")).IsNull();
        _ = await Assert.That(RatingMath.ClampText(null)).IsNull();
    }

    [Test]
    public async Task ClampText_CutsAtTheStoredLength()
    {
        var clamped = RatingMath.ClampText(new string('a', ContributionsDbContext.MaxTextLength + 10));

        _ = await Assert.That(clamped.Length).IsEqualTo(ContributionsDbContext.MaxTextLength);
    }

    [Test]
    public async Task Distribution_CountsWholeStarsByBucket()
    {
        var buckets = RatingMath.Distribution([5m, 5m, 4m, 3m, 1m]);

        _ = await Assert.That(string.Join(",", buckets)).IsEqualTo("1,0,1,1,2");
    }

    [Test]
    public async Task Distribution_RoundsHalfStarsDown()
    {
        var buckets = RatingMath.Distribution([4.5m, 3.5m, 1.5m]);

        _ = await Assert.That(string.Join(",", buckets)).IsEqualTo("1,0,1,1,0");
    }

    [Test]
    public async Task Distribution_PutsALoneHalfStarInOneStar()
    {
        var buckets = RatingMath.Distribution([0.5m]);

        _ = await Assert.That(buckets[0]).IsEqualTo(1);
    }
}
