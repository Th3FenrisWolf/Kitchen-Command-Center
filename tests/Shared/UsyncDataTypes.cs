using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace KCC.Tests;

internal static class UsyncDataTypes
{
    public static string[] DropdownItems(string dataType)
    {
        var path = Path.Combine(RepoPaths.WebProject, "uSync", "v17", "DataTypes", $"{dataType}.config");
        var config = XDocument.Load(path).Root!.Element("Config")!.Value;
        return JsonNode.Parse(config)!["items"]!.AsArray().GetValues<string>().ToArray();
    }
}
