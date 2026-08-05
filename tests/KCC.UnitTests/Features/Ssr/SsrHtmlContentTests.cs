using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Models.Common;

namespace KCC.UnitTests.Features.Ssr;

public class SsrHtmlContentTests
{
    private const string SsrErrorScriptOpen = "<script id=\"ssr-error\" type=\"application/json\">";

    [Test]
    public async Task WriteTo_WithoutError_OmitsSsrErrorScript()
    {
        var html = Render(new SsrResult { HeaderContent = "h", BodyContent = "b", FooterContent = "f" });

        _ = await Assert.That(html.Contains("ssr-error")).IsFalse();
    }

    [Test]
    public async Task WriteTo_WithError_EmitsSsrErrorJson()
    {
        var html = Render(new SsrResult
        {
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            ErrorMessage = "window is not defined",
            ErrorStack = "at render (/Features/App.vue:3:1)",
        });

        using var doc = JsonDocument.Parse(ExtractSsrErrorJson(html));

        _ = await Assert.That(doc.RootElement.GetProperty("message").GetString()).IsEqualTo("window is not defined");
        _ = await Assert.That(doc.RootElement.GetProperty("stack").GetString()).IsEqualTo("at render (/Features/App.vue:3:1)");
    }

    [Test]
    public async Task WriteTo_ErrorContainingMarkup_IsEscapedInsideScript()
    {
        var html = Render(new SsrResult
        {
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            ErrorMessage = "bad </script><img src=x>",
        });

        _ = await Assert.That(html.Contains("</script><img")).IsFalse();

        using var doc = JsonDocument.Parse(ExtractSsrErrorJson(html));

        _ = await Assert.That(doc.RootElement.GetProperty("message").GetString()).IsEqualTo("bad </script><img src=x>");
        _ = await Assert.That(doc.RootElement.GetProperty("stack").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    private static string Render(SsrResult result)
    {
        using var writer = new StringWriter();
        new SsrHtmlContent(result).WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static string ExtractSsrErrorJson(string html)
    {
        var start = html.IndexOf(SsrErrorScriptOpen, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException("ssr-error script not found");
        }

        var jsonStart = start + SsrErrorScriptOpen.Length;
        var end = html.IndexOf("</script>", jsonStart, StringComparison.Ordinal);
        return html[jsonStart..end];
    }
}
