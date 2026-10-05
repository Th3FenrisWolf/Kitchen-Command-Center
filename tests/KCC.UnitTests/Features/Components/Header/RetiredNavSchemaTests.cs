using System.Xml.Linq;

namespace KCC.UnitTests.Features.Components.Header;

// Deleting a type's config file leaves the type in every database that already imported it. uSync deletes it only when
// a marker stands in for the file.
public class RetiredNavSchemaTests
{
    private static readonly string[] RetiredKeys =
    [
        "ff558bff-77ca-8166-bdc9-0f175d319c29",
        "672e1703-aeab-87d1-b3a0-ac8094e92775",
        "ca49810a-9594-8d76-be41-f6d21f3769d9",
        "01201564-9552-847e-8b31-92ca261480f6",
        "d658755a-a6e5-8b5c-870a-3092e6cdbe60",
    ];

    [Test]
    [Arguments("ContentTypes", "navlink.config", "ff558bff-77ca-8166-bdc9-0f175d319c29", "navLink")]
    [Arguments("ContentTypes", "navgroup.config", "672e1703-aeab-87d1-b3a0-ac8094e92775", "navGroup")]
    [Arguments("DataTypes", "KCCNavItems.config", "ca49810a-9594-8d76-be41-f6d21f3769d9", "KCC Nav Items")]
    [Arguments("DataTypes", "KCCShowWhen.config", "01201564-9552-847e-8b31-92ca261480f6", "KCC Show When")]
    [Arguments("DataTypes", "KCCLinks.config", "d658755a-a6e5-8b5c-870a-3092e6cdbe60", "KCC Links")]
    public async Task RetiredType_IsADeleteMarker(string folder, string file, string key, string alias)
    {
        var marker = XDocument.Load(Path.Combine(RepoPaths.WebProject, "uSync", "v17", folder, file)).Root;

        _ = await Assert.That(marker.Name.LocalName).IsEqualTo("Empty");
        _ = await Assert.That(marker.Attribute("Key").Value).IsEqualTo(key);
        _ = await Assert.That(marker.Attribute("Alias").Value).IsEqualTo(alias);
        _ = await Assert.That(marker.Attribute("Change").Value).IsEqualTo("Delete");
    }

    [Test]
    public async Task NoSchemaStillUsesARetiredType()
    {
        var users = Directory.GetFiles(Path.Combine(RepoPaths.WebProject, "uSync", "v17"), "*.config", SearchOption.AllDirectories)
            .Where(path => XDocument.Load(path).Root.Name.LocalName != "Empty")
            .Where(path => RetiredKeys.Any(key => File.ReadAllText(path).Contains(key, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(RepoPaths.WebProject, path));

        _ = await Assert.That(users).IsEmpty();
    }
}
