using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Helpers;

public static class JsonSerializer
{
    // The owner can type this JSON by hand in the backoffice, and a slip should empty the list rather than take a
    // page or a recipe's search document down.
    public static IEnumerable<T> DeserializeCollection<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<IEnumerable<T>>(json, JsonNaming.CamelCase) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
