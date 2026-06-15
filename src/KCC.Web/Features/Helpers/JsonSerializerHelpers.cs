using System.Text.Json;
using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Helpers;

public static class JsonSerializer
{
    public static IEnumerable<T> DeserializeCollection<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return System.Text.Json.JsonSerializer.Deserialize<IEnumerable<T>>(json, JsonNaming.CamelCase) ?? [];
    }
}
