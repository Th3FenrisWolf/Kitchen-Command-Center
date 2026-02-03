using System.Text.Encodings.Web;
using System.Text.Json;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KCC.Web.Extensions;

public static class HtmlHelperExtensions
{
    public static readonly JsonSerializerOptions SerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // Use relaxed encoding to preserve Unicode characters (like · and —)
        // without escaping them to \uXXXX sequences
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static EditableAreaOptions GetAreaOptions(this IHtmlHelper _, string[] allowedWidgets = null, string defaultSectionIdentifier = null)
    {
        var hasWidgetRestrictions = allowedWidgets?.Length > 0;

        return new()
        {
            AllowedWidgets = hasWidgetRestrictions ? allowedWidgets : AllowedComponents.ALL,
            AllowWidgetOutputCache = true,
            DefaultSectionIdentifier = defaultSectionIdentifier,
            WidgetOutputCacheExpiresSliding = TimeSpan.FromMinutes(2),
        };
    }

    public static string Clsx(this IHtmlHelper htmlHelper, params object[] classNames)
    {
        var classList = classNames.SelectMany(cName => cName switch
        {
            string str when !string.IsNullOrWhiteSpace(str) => [str],
            IEnumerable<object> collection => collection.SelectMany(item => htmlHelper.Clsx(item).Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            (bool condition, object value) => condition ? [value.ToString()] : [],
            _ => []
        });

        return string.Join(" ", classList.Distinct());
    }

    /// <summary>
    /// Serializes a value to JSON for use in Vue component props.
    /// The output is safe for HTML attributes while preserving Unicode characters.
    /// </summary>
    /// <param name="_">The HTML helper (unused, required for extension method syntax).</param>
    /// <param name="value">The value to serialize as JSON.</param>
    /// <returns>HTML-safe JSON string for use in Vue component props.</returns>
    public static IHtmlContent VueProp(this IHtmlHelper _, object value)
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
