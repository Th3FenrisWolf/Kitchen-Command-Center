using System.Text.Json.Nodes;
using System.Xml.Linq;
using KCC.Web.Features.Recipes;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.Recipes;

public class TagKindsTests
{
    [Test]
    public async Task TagKindOptions_AreTheKindsTheCodeReads()
    {
        var path = Path.Combine(RepoPaths.WebProject, "uSync", "v17", "DataTypes", "KCCTagKind.config");
        var config = XDocument.Load(path).Root.Element("Config").Value;
        var items = JsonNode.Parse(config)["items"].AsArray().Select(item => (string)item);

        _ = await Assert.That(items).IsEquivalentTo([TagKinds.Diet, TagKinds.Style], CollectionOrdering.Matching);
    }
}
