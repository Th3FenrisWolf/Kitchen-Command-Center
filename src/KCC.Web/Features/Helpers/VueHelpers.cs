using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

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

    /// <summary>
    /// Serializes a value to JSON for use in Vue component props.
    /// The output is safe for HTML attributes while preserving Unicode characters.
    /// </summary>
    /// <param name="value">The value to serialize as JSON.</param>
    /// <returns>HTML-safe JSON string for use in Vue component props.</returns>
    public static IHtmlContent Prop(object value)
    {
        var json = JsonSerializer.Serialize(value, SerializationOptions);

        // HTML-encode only the characters that break HTML attribute parsing
        // while preserving Unicode characters (like · and —) as-is.
        // Order matters: encode & first to avoid double-encoding.
        var htmlSafe = json
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return new HtmlString(htmlSafe);
    }
}
