using KCC.Contributions.Admin;

namespace KCC.UnitTests.Features.Contributions;

public class VariantNameLookupTests
{
    [Test]
    public async Task DisplayOrDeleted_ReturnsNameWhenPresent()
    {
        var v = Guid.NewGuid();
        var map = new Dictionary<Guid, string> { [v] = "Spicy Mac" };
        _ = await Assert.That(VariantNameLookup.DisplayOrDeleted(map, v)).IsEqualTo("Spicy Mac");
    }

    [Test]
    public async Task DisplayOrDeleted_FallsBackForMissing()
    {
        _ = await Assert.That(VariantNameLookup.DisplayOrDeleted(new Dictionary<Guid, string>(), Guid.NewGuid())).IsEqualTo("(deleted)");
    }
}
