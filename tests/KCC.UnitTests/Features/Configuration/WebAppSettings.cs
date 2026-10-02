using System.Text.Json;

namespace KCC.UnitTests.Features.Configuration;

internal static class WebAppSettings
{
    public static JsonElement Load(string fileName = "appsettings.json") =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoPaths.WebProject, fileName))).RootElement;
}
