using KCC.Contributions.Admin;

namespace KCC.UnitTests.Features.Contributions;

public class MemberNameLookupTests
{
    [Test]
    public async Task DisplayOrDeleted_ReturnsNameWhenPresent()
    {
        var map = new Dictionary<Guid, string> { [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "Ana Cook" };
        var name = MemberNameLookup.DisplayOrDeleted(map, Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _ = await Assert.That(name).IsEqualTo("Ana Cook");
    }

    [Test]
    public async Task DisplayOrDeleted_FallsBackForMissing()
    {
        var map = new Dictionary<Guid, string>();
        var name = MemberNameLookup.DisplayOrDeleted(map, Guid.NewGuid());
        _ = await Assert.That(name).IsEqualTo("(deleted)");
    }
}
