using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.Web.Features.Ssr;

/// <summary>
/// HTML helper extensions for Vue SSR rendering.
/// </summary>
public static class SsrHtmlHelperExtensions
{
    /// <summary>
    /// Renders the page content with Vue SSR.
    /// If SSR is unavailable, falls back to client-side rendering.
    /// </summary>
    /// <param name="html">The HTML helper.</param>
    /// <param name="bodyContent">The body content from RenderBody().</param>
    /// <returns>The SSR-rendered HTML wrapped in the app container.</returns>
    public static async Task<IHtmlContent> RenderVueSsrAsync(
        this IHtmlHelper html,
        IHtmlContent bodyContent)
    {
        var ssrService = html.ViewContext.HttpContext.RequestServices
            .GetRequiredService<VueSsrService>();

        // Get cancellation token from request
        var cancellationToken = html.ViewContext.HttpContext.RequestAborted;

        // Capture the body content as a string
        var serverContent = RenderHtmlContentToString(bodyContent);

        // Call SSR service
        var result = await ssrService.RenderAsync(serverContent, cancellationToken);

        return new SsrHtmlContent(result);
    }

    private static string RenderHtmlContentToString(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
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
            writer.Write(System.Text.Json.JsonSerializer.Serialize(this.result.ServerContent));
            writer.Write("</script>");
        }
        else
        {
            // SSR failed or disabled - let client render
            writer.Write("<div id=\"app\"></div>");

            // Server content goes in a script tag as JSON to prevent XSS
            writer.Write("<script id=\"server-content\" type=\"application/json\">");
            writer.Write(System.Text.Json.JsonSerializer.Serialize(this.result.ServerContent));
            writer.Write("</script>");
        }
    }
}
