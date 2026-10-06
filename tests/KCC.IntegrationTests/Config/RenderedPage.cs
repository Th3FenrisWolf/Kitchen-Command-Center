using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KCC.IntegrationTests.Config;

// The test host renders with SSR off, so a page's header and body arrive as Vue template text inside the server-content
// JSON, and each one's root component carries its view model as attributes.
public sealed class RenderedPage
{
    private const string ServerContentOpen = "<script id=\"server-content\" type=\"application/json\">";

    private RenderedPage(HttpStatusCode status, string html, string header, string body)
    {
        Status = status;
        Html = html;
        Header = header;
        Body = body;
    }

    public HttpStatusCode Status { get; }

    public string Html { get; }

    public string Header { get; }

    public string Body { get; }

    public static async Task<RenderedPage> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var header = string.Empty;
        var body = string.Empty;
        var start = html.IndexOf(ServerContentOpen, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += ServerContentOpen.Length;
            var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
            using var content = JsonDocument.Parse(html[start..end]);
            header = content.RootElement.GetProperty("headerContent").GetString() ?? string.Empty;
            body = content.RootElement.GetProperty("bodyContent").GetString() ?? string.Empty;
        }

        return new RenderedPage(response.StatusCode, html, header, body);
    }

    public string? Attribute(string name) => AttributeIn(Body, name);

    public JsonElement Prop(string name) => PropIn(Body, name);

    public JsonElement HeaderProp(string name) => PropIn(Header, name);

    private static string? AttributeIn(string template, string name)
    {
        var match = Regex.Match(template, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    private static JsonElement PropIn(string template, string name)
    {
        var json = AttributeIn(template, $":{name}") ?? throw new InvalidOperationException($"The page has no :{name} prop.");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
