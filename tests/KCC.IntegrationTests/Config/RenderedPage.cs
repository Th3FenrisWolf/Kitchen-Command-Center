using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KCC.IntegrationTests.Config;

// The test host renders with SSR off, so a page's body arrives as the Vue template text inside the
// server-content JSON, and its root component carries the view model as attributes.
public sealed class RenderedPage
{
    private const string ServerContentOpen = "<script id=\"server-content\" type=\"application/json\">";

    private RenderedPage(HttpStatusCode status, string html, string body)
    {
        Status = status;
        Html = html;
        Body = body;
    }

    public HttpStatusCode Status { get; }

    public string Html { get; }

    public string Body { get; }

    public static async Task<RenderedPage> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var body = string.Empty;
        var start = html.IndexOf(ServerContentOpen, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += ServerContentOpen.Length;
            var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
            using var content = JsonDocument.Parse(html[start..end]);
            body = content.RootElement.GetProperty("bodyContent").GetString() ?? string.Empty;
        }

        return new RenderedPage(response.StatusCode, html, body);
    }

    public string? Attribute(string name)
    {
        var match = Regex.Match(Body, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    public JsonElement Prop(string name)
    {
        var json = Attribute($":{name}") ?? throw new InvalidOperationException($"The page has no :{name} prop.");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
