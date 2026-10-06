using System.Xml.Linq;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.Recipes;

public class TaxonomyDefaultsTests
{
    [Test]
    public async Task Values_MatchTheBaselineContent()
    {
        var baseline = BaselineNodes();
        var mismatched = TaxonomyDefaults.Values
            .Where(value => !baseline.TryGetValue(value.Key, out var node)
                || node.Element("Info").Element("NodeName").Attribute("Default").Value != value.Name
                || node.Element("Properties")?.Element(value.Alias)?.Element("Value")?.Value != value.Stored)
            .Select(value => value.Name);

        _ = await Assert.That(mismatched).IsEmpty();
    }

    [Test]
    public async Task Values_CoverEveryBaselineTagAndCategory()
    {
        var taxonomy = BaselineNodes()
            .Where(node => node.Value.Element("Info").Element("ContentType").Value is "recipeTag" or "recipeCategory")
            .Select(node => node.Key);

        _ = await Assert.That(taxonomy).IsEquivalentTo(TaxonomyDefaults.Values.Select(value => value.Key));
    }

    private static Dictionary<Guid, XElement> BaselineNodes() =>
        Directory.GetFiles(Path.Combine(RepoPaths.WebProject, "uSync", "v17", "Content"), "*.config")
            .Select(path => XDocument.Load(path).Root)
            .ToDictionary(root => new Guid(root.Attribute("Key").Value));
}
