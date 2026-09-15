using KCC.Contributions.Admin;

namespace KCC.UnitTests.Features.Contributions;

public class ContentItemNameLookupTests
{
    private const int DefaultLanguageId = 1;

    [Test]
    public async Task DisplayOrDeleted_ReturnsNameWhenPresent()
    {
        var v = Guid.NewGuid();
        var map = new Dictionary<Guid, string> { [v] = "Spicy Mac" };
        _ = await Assert.That(ContentItemNameLookup.DisplayOrDeleted(map, v)).IsEqualTo("Spicy Mac");
    }

    [Test]
    public async Task DisplayOrDeleted_FallsBackForMissing()
    {
        _ = await Assert.That(ContentItemNameLookup.DisplayOrDeleted(new Dictionary<Guid, string>(), Guid.NewGuid())).IsEqualTo("(deleted)");
    }

    [Test]
    public async Task Merge_PrefersDefaultLanguageDisplayName()
    {
        var guid = Guid.NewGuid();
        var map = ContentItemNameLookup.Merge(
            [(1, guid, "12-hoursteep-hqmmn7m7")],
            [(1, 3, "12-Stunden-Aufguss"), (1, DefaultLanguageId, "12-Hour Steep")],
            DefaultLanguageId);

        _ = await Assert.That(map[guid]).IsEqualTo("12-Hour Steep");
    }

    [Test]
    public async Task Merge_FallsBackToLowestOtherLanguageWhenDefaultMissing()
    {
        var guid = Guid.NewGuid();
        var map = ContentItemNameLookup.Merge(
            [(1, guid, "codename")],
            [(1, 5, "Spanish Name"), (1, 3, "German Name")],
            DefaultLanguageId);

        _ = await Assert.That(map[guid]).IsEqualTo("German Name");
    }

    [Test]
    public async Task Merge_FallsBackToCodeNameWhenNoMetadata()
    {
        var guid = Guid.NewGuid();
        var map = ContentItemNameLookup.Merge(
            [(1, guid, "12-hoursteep-hqmmn7m7")],
            [],
            DefaultLanguageId);

        _ = await Assert.That(map[guid]).IsEqualTo("12-hoursteep-hqmmn7m7");
    }

    [Test]
    public async Task Merge_SkipsBlankDisplayNames()
    {
        var guid = Guid.NewGuid();
        var map = ContentItemNameLookup.Merge(
            [(1, guid, "codename")],
            [(1, DefaultLanguageId, "   "), (1, 3, "German Name")],
            DefaultLanguageId);

        _ = await Assert.That(map[guid]).IsEqualTo("German Name");
    }

    [Test]
    public async Task Merge_MapsEachItemIndependently()
    {
        var withMetadata = Guid.NewGuid();
        var withoutMetadata = Guid.NewGuid();
        var map = ContentItemNameLookup.Merge(
            [(1, withMetadata, "code-a"), (2, withoutMetadata, "code-b")],
            [(1, DefaultLanguageId, "Friendly A")],
            DefaultLanguageId);

        _ = await Assert.That(map[withMetadata]).IsEqualTo("Friendly A");
        _ = await Assert.That(map[withoutMetadata]).IsEqualTo("code-b");
    }
}
