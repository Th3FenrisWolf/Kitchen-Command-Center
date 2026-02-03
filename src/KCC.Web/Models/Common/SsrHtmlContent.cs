using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace KCC.Web.Models.Common;

public class SsrHtmlContent(SsrResult result) : IHtmlContent
{
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        writer.Write("<div id=\"app\">");

        if (result.Html is not null)
        {
            // SSR succeeded - render the pre-rendered HTML
            writer.Write(result.Html); // Already HTML from SSR, no need to encode
        }

        writer.Write("</div>");

        // Server content goes in a script tag as JSON to prevent XSS
        writer.Write($"<script id=\"server-content\" type=\"application/json\">");

        // Encode the content as JSON string to escape any dangerous characters
        writer.Write(JsonSerializer.Serialize(new
        {
            headerContent = result.HeaderContent,
            bodyContent = result.BodyContent,
            footerContent = result.FooterContent,
        }));

        writer.Write("</script>");
    }
}
