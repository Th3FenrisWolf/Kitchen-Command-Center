using KCC.Web.Features.Components.Header;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.Components.Header;

public class NavPresetsTests
{
    [Test]
    public async Task PresetOptions_AreThePresetsTheCodeKnows()
    {
        var items = UsyncDataTypes.DropdownItems("KCCNavPreset");

        _ = await Assert.That(items).IsEquivalentTo(NavPresets.All, CollectionOrdering.Matching);
    }
}
