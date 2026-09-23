using KCC.Contributions.Admin;

namespace KCC.UnitTests.Features.Contributions;

public class MemberNameLookupTests
{
    private static readonly Guid MemberGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Format_UsesFullNameWhenPresent()
    {
        _ = await Assert.That(MemberNameLookup.Format("Ana", "Cook", "anacook")).IsEqualTo("Ana Cook");
    }

    [Test]
    public async Task Format_UsesPartialNameWhenOnlyOnePartPresent()
    {
        _ = await Assert.That(MemberNameLookup.Format("Ana", "", "anacook")).IsEqualTo("Ana");
        _ = await Assert.That(MemberNameLookup.Format(null, "Cook", "anacook")).IsEqualTo("Cook");
    }

    [Test]
    public async Task Format_FallsBackToUserName()
    {
        _ = await Assert.That(MemberNameLookup.Format("", "  ", "anacook")).IsEqualTo("anacook");
    }

    [Test]
    public async Task Format_FallsBackToDeletedWhenAllBlank()
    {
        _ = await Assert.That(MemberNameLookup.Format(null, null, " ")).IsEqualTo("(deleted)");
    }

    [Test]
    public async Task DisplayOrDeleted_ReturnsNameWhenPresent()
    {
        var map = new Dictionary<Guid, MemberDisplay> { [MemberGuid] = new("Ana Cook", "ana@example.com") };
        _ = await Assert.That(MemberNameLookup.DisplayOrDeleted(map, MemberGuid)).IsEqualTo("Ana Cook");
    }

    [Test]
    public async Task DisplayOrDeleted_FallsBackForMissing()
    {
        _ = await Assert.That(MemberNameLookup.DisplayOrDeleted(new Dictionary<Guid, MemberDisplay>(), Guid.NewGuid())).IsEqualTo("(deleted)");
    }

    [Test]
    public async Task DisplayWithEmail_IncludesEmailWhenPresent()
    {
        var map = new Dictionary<Guid, MemberDisplay> { [MemberGuid] = new("Ana Cook", "ana@example.com") };
        _ = await Assert.That(MemberNameLookup.DisplayWithEmail(map, MemberGuid)).IsEqualTo("Ana Cook (ana@example.com)");
    }

    [Test]
    public async Task DisplayWithEmail_OmitsEmptyEmail()
    {
        var map = new Dictionary<Guid, MemberDisplay> { [MemberGuid] = new("Ana Cook", "") };
        _ = await Assert.That(MemberNameLookup.DisplayWithEmail(map, MemberGuid)).IsEqualTo("Ana Cook");
    }

    [Test]
    public async Task DisplayWithEmail_FallsBackForMissing()
    {
        _ = await Assert.That(MemberNameLookup.DisplayWithEmail(new Dictionary<Guid, MemberDisplay>(), Guid.NewGuid())).IsEqualTo("(deleted)");
    }
}
