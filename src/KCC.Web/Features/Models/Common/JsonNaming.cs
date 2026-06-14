using System.Text.Json;

namespace KCC.Web.Features.Models.Common;

public static class JsonNaming
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
