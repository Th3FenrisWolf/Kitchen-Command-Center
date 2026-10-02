using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace KCC.Web.Features.Models.Common;

public class SsrHtmlContent(SsrResult result) : IHtmlContent
{
    internal const string ServerContentScriptOpen =
        "<script id=\"server-content\" type=\"application/json\">";

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        // Razor has already flushed <head> by the time this runs in the body, and browsers apply
        // body styles at parse time, so the app markup never paints unstyled either way.
        if (!string.IsNullOrEmpty(result.Css))
        {
            writer.Write("<style data-ssr-styles>");

            // A literal "</style" in the CSS would close the tag early.
            writer.Write(result.Css.Replace("</style", "<\\/style", StringComparison.OrdinalIgnoreCase));
            writer.Write("</style>");
        }

        writer.Write("<div id=\"app\">");

        if (result.Html is not null)
        {
            writer.Write(result.Html); // Already HTML from SSR, no need to encode
        }

        writer.Write("</div>");

        // Server content goes in a script tag as JSON to prevent XSS
        writer.Write(ServerContentScriptOpen);

        writer.Write(JsonSerializer.Serialize(new
        {
            headerContent = result.HeaderContent,
            bodyContent = result.BodyContent,
            footerContent = result.FooterContent,
            isPreview = result.IsPreview,
        }));

        writer.Write("</script>");

        if (result.ErrorMessage is not null)
        {
            writer.Write("<script id=\"ssr-error\" type=\"application/json\">");
            writer.Write(JsonSerializer.Serialize(new { message = result.ErrorMessage, stack = result.ErrorStack }));
            writer.Write("</script>");
        }
    }
}
