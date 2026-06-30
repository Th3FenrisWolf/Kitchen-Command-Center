using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class VariantCookedAggregationTests
{
    [Test]
    public async Task CountByVariant_GroupsRows()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var map = VariantCookedInfoProvider.CountByVariant(new[] { v1, v1, v2 });
        _ = await Assert.That(map[v1]).IsEqualTo(2);
        _ = await Assert.That(map[v2]).IsEqualTo(1);
    }

    [Test]
    public async Task CountByVariant_EmptyForNoRows()
    {
        var map = VariantCookedInfoProvider.CountByVariant(Array.Empty<Guid>());
        _ = await Assert.That(map.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RecipeCount_CountsAllRows()
    {
        _ = await Assert.That(VariantCookedInfoProvider.CountRows(new[] { Guid.NewGuid(), Guid.NewGuid() })).IsEqualTo(2);
    }
}
