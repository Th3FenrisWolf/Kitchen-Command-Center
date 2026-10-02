using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

using Serializer = System.Text.Json.JsonSerializer;

namespace KCC.Web.Features.Helpers;

public static class Vue
{
    public static readonly JsonSerializerOptions SerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // Use relaxed encoding to preserve Unicode characters (like · and —)
        // without escaping them to \uXXXX sequences
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IHtmlContent Prop(object value)
    {
        var json = Serializer.Serialize(value, SerializationOptions);

        // Only what breaks an attribute is encoded, so · and — stay as they are; & goes first so the entities after it
        // are not encoded twice.
        var htmlSafe = json
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return new HtmlString(htmlSafe);
    }
}
