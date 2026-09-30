using System.Text.Json;

namespace KCC.UnitTests.Features.Configuration;

internal static class WebAppSettings
{
    public static JsonElement Load(string fileName = "appsettings.json")
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        var path = Path.Combine(directory!.FullName, "src", "KCC.Web", fileName);
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }
}
