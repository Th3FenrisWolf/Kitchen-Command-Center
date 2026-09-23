using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class CookNoteAggregationTests
{
    [Test]
    public async Task CountByVariant_CountsPerVariant()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var counts = VariantCookNoteInfoProvider.CountByVariant([a, b, a, a]);

        _ = await Assert.That(counts[a]).IsEqualTo(3);
        _ = await Assert.That(counts[b]).IsEqualTo(1);
    }

    [Test]
    public async Task CountByVariant_EmptyInputYieldsEmptyMap()
    {
        var counts = VariantCookNoteInfoProvider.CountByVariant([]);

        _ = await Assert.That(counts.Count).IsEqualTo(0);
    }
}
