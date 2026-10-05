using KCC.Web.Features.Recipes;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.Recipes;

public class TagKindsTests
{
    [Test]
    public async Task TagKindOptions_AreTheKindsTheCodeReads()
    {
        var items = UsyncDataTypes.DropdownItems("KCCTagKind");

        _ = await Assert.That(items).IsEquivalentTo([TagKinds.Diet, TagKinds.Style], CollectionOrdering.Matching);
    }
}
