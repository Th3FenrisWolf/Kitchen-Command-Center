using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KCC.Web.Features.Ssr;

/// <summary>
/// HTML helper extensions for Vue SSR rendering.
/// </summary>
public static class SsrHtmlHelperExtensions
{
    /// <summary>
    /// Renders the page content with Vue SSR using separate content regions.
    /// If SSR is unavailable, falls back to client-side rendering.
    /// </summary>
    /// <param name="html">The HTML helper.</param>
    /// <param name="headerContent">The header content from ViewComponent.</param>
    /// <param name="bodyContent">The main page body content from RenderBody().</param>
    /// <param name="footerContent">The footer content from ViewComponent.</param>
    /// <returns>The SSR-rendered HTML wrapped in the app container.</returns>
    public static async Task<IHtmlContent> RenderVueSsrAsync(
        this IHtmlHelper html,
        IHtmlContent headerContent,
        IHtmlContent bodyContent,
        IHtmlContent footerContent)
    {
        var ssrService = html.ViewContext.HttpContext.RequestServices
            .GetRequiredService<VueSsrService>();

        // Get cancellation token from request
        var cancellationToken = html.ViewContext.HttpContext.RequestAborted;

        // Capture each content region as a string, reusing a single StringBuilder
        var (header, body, footer) = RenderContentRegions(headerContent, bodyContent, footerContent);

        // Call SSR service with separate regions
        var result = await ssrService.RenderAsync(header, body, footer, cancellationToken);

        return new SsrHtmlContent(result);
    }

    private static (string Header, string Body, string Footer) RenderContentRegions(
        IHtmlContent headerContent,
        IHtmlContent bodyContent,
        IHtmlContent footerContent)
    {
        // Reuse a single StringBuilder with pre-allocated capacity
        var sb = new StringBuilder(8192);
        using var writer = new StringWriter(sb);
        var encoder = HtmlEncoder.Default;

        // Render header
        headerContent.WriteTo(writer, encoder);
        var header = sb.ToString();
        sb.Clear();

        // Render body
        bodyContent.WriteTo(writer, encoder);
        var body = sb.ToString();
        sb.Clear();

        // Render footer
        footerContent.WriteTo(writer, encoder);
        var footer = sb.ToString();

        return (header, body, footer);
    }
}

/// <summary>
/// HTML content wrapper for SSR output.
/// </summary>
internal sealed class SsrHtmlContent : IHtmlContent
{
    private readonly SsrResult result;

    public SsrHtmlContent(SsrResult result)
    {
        this.result = result;
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        // Create the content regions object for client-side hydration
        var contentRegions = new
        {
            headerContent = this.result.HeaderContent,
            bodyContent = this.result.BodyContent,
            footerContent = this.result.FooterContent,
        };

        if (this.result.WasServerRendered)
        {
            // SSR succeeded - render the pre-rendered HTML
            writer.Write("<div id=\"app\">");
            writer.Write(this.result.Html); // Already HTML from SSR, don't encode
            writer.Write("</div>");

            // Server content goes in a script tag as JSON to prevent XSS
            // The client will parse this safely
            writer.Write("<script id=\"server-content\" type=\"application/json\">");
            // Encode the content as JSON string to escape any dangerous characters
            writer.Write(System.Text.Json.JsonSerializer.Serialize(contentRegions));
            writer.Write("</script>");
        }
        else
        {
            // SSR failed or disabled - let client render
            writer.Write("<div id=\"app\"></div>");

            // Server content goes in a script tag as JSON to prevent XSS
            writer.Write("<script id=\"server-content\" type=\"application/json\">");
            writer.Write(System.Text.Json.JsonSerializer.Serialize(contentRegions));
            writer.Write("</script>");
        }
    }
}
