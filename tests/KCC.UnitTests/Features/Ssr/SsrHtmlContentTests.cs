using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Bloom;
using KCC.Web.Features.Islands;
using KCC.Web.Features.Models.Common;

namespace KCC.UnitTests.Features.Ssr;

public class SsrHtmlContentTests
{
    private const string SsrErrorScriptOpen = "<script id=\"ssr-error\" type=\"application/json\">";
    private const string ServerContentScriptOpen = "<script id=\"server-content\" type=\"application/json\">";

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

    [Test]
    public async Task WriteTo_WithoutCss_OmitsStyleTag()
    {
        var html = Render(new SsrResult { HeaderContent = "h", BodyContent = "b", FooterContent = "f" });

        _ = await Assert.That(html.Contains("<style")).IsFalse();
    }

    [Test]
    public async Task WriteTo_WithCss_EmitsStyleTagBeforeApp()
    {
        var html = Render(new SsrResult
        {
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            Css = ".recipe-card-notch[data-v-1567288a]{clip-path:none}",
        });

        var styleIndex = html.IndexOf("<style data-ssr-styles>", StringComparison.Ordinal);

        _ = await Assert.That(styleIndex).IsGreaterThanOrEqualTo(0);
        _ = await Assert.That(styleIndex).IsLessThan(html.IndexOf("<div id=\"app\">", StringComparison.Ordinal));
        _ = await Assert.That(html).Contains(".recipe-card-notch[data-v-1567288a]{clip-path:none}");
    }

    [Test]
    public async Task WriteTo_CssContainingClosingStyleTag_NeutralizesSequence()
    {
        var html = Render(new SsrResult
        {
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            Css = "a{content:\"</style><img src=x>\"}",
        });

        _ = await Assert.That(html.Contains("</style><img")).IsFalse();
        _ = await Assert.That(html).Contains("<\\/style><img src=x>");
    }

    [Test]
    public async Task WriteTo_ServerContent_CarriesTheBloomSeedAsAnUnsignedIntegerAndThePage()
    {
        var html = Render(new SsrResult
        {
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            Bloom = new BloomInputs(4294967295, "/recipes/brown-butter-gnocchi"),
        });

        using var doc = JsonDocument.Parse(ExtractJson(html, ServerContentScriptOpen));
        var bloom = doc.RootElement.GetProperty("bloom");

        _ = await Assert.That(bloom.GetProperty("seed").GetRawText()).IsEqualTo("4294967295");
        _ = await Assert.That(bloom.GetProperty("page").GetString()).IsEqualTo("/recipes/brown-butter-gnocchi");
    }

    [Test]
    public async Task WriteTo_WithRenderedHtml_CarriesTheBloomAndIslandsAndNoRegionTemplates()
    {
        var html = Render(new SsrResult
        {
            Html = "<header></header>",
            HeaderContent = "h",
            BodyContent = "b",
            FooterContent = "f",
            IsPreview = true,
            Bloom = new BloomInputs(7, "/recipes/"),
            PageWashes = 5,
            Islands = [new IslandEntry("Stacker", Props("""{"recipeId":3,"title":"Gnocchi"}"""), ["/recipes/|stacker", "@pad~2"])],
        });

        using var doc = JsonDocument.Parse(ExtractJson(html, ServerContentScriptOpen));
        var root = doc.RootElement;
        var island = root.GetProperty("islands")[0];

        _ = await Assert.That(root.EnumerateObject().Select(p => p.Name).ToArray()).IsEquivalentTo(["isPreview", "bloom", "islands"]);
        _ = await Assert.That(root.GetProperty("isPreview").GetBoolean()).IsTrue();
        _ = await Assert.That(root.GetProperty("bloom").GetProperty("seed").GetRawText()).IsEqualTo("7");
        _ = await Assert.That(root.GetProperty("bloom").GetProperty("page").GetString()).IsEqualTo("/recipes/");
        _ = await Assert.That(root.GetProperty("bloom").GetProperty("pageWashes").GetInt32()).IsEqualTo(5);
        _ = await Assert.That(root.GetProperty("islands").GetArrayLength()).IsEqualTo(1);
        _ = await Assert.That(island.GetProperty("name").GetString()).IsEqualTo("Stacker");
        _ = await Assert.That(island.GetProperty("props").GetProperty("recipeId").GetInt32()).IsEqualTo(3);
        _ = await Assert.That(island.GetProperty("props").GetProperty("title").GetString()).IsEqualTo("Gnocchi");
        _ = await Assert.That(island.GetProperty("keys").EnumerateArray().Select(k => k.GetString()).ToArray())
            .IsEquivalentTo(["/recipes/|stacker", "@pad~2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task WriteTo_WithoutRenderedHtml_CarriesTheThreeRegionTemplates()
    {
        var html = Render(new SsrResult { HeaderContent = "h", BodyContent = "b", FooterContent = "f", Bloom = new BloomInputs(7, "/") });

        using var doc = JsonDocument.Parse(ExtractJson(html, ServerContentScriptOpen));
        var root = doc.RootElement;

        _ = await Assert.That(root.GetProperty("headerContent").GetString()).IsEqualTo("h");
        _ = await Assert.That(root.GetProperty("bodyContent").GetString()).IsEqualTo("b");
        _ = await Assert.That(root.GetProperty("footerContent").GetString()).IsEqualTo("f");
        _ = await Assert.That(root.TryGetProperty("islands", out _)).IsFalse();
    }

    [Test]
    public async Task WriteTo_IslandPropHoldingMarkup_IsEscapedSoTheScriptEndsAtItsOwnClosingTag()
    {
        const string hostile = "</script><script>alert(1)</script><!--";
        var html = Render(new SsrResult
        {
            Html = "<main></main>",
            Bloom = new BloomInputs(7, "/"),
            Islands = [new IslandEntry("Stacker", Props(JsonSerializer.Serialize(new { title = hostile })), [])],
        });

        var json = ExtractJson(html, ServerContentScriptOpen);
        var afterPayload = html[(html.IndexOf(ServerContentScriptOpen, StringComparison.Ordinal) + ServerContentScriptOpen.Length + json.Length)..];

        _ = await Assert.That(json.Contains("<!--")).IsFalse();
        _ = await Assert.That(afterPayload).IsEqualTo("</script>");

        using var doc = JsonDocument.Parse(json);

        _ = await Assert.That(doc.RootElement.GetProperty("islands")[0].GetProperty("props").GetProperty("title").GetString()).IsEqualTo(hostile);
    }

    private static JsonElement Props(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static string Render(SsrResult result)
    {
        using var writer = new StringWriter();
        new SsrHtmlContent(result).WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static string ExtractSsrErrorJson(string html) => ExtractJson(html, SsrErrorScriptOpen);

    private static string ExtractJson(string html, string scriptOpen)
    {
        var start = html.IndexOf(scriptOpen, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"{scriptOpen} not found");
        }

        var jsonStart = start + scriptOpen.Length;
        var end = html.IndexOf("</script>", jsonStart, StringComparison.Ordinal);
        return html[jsonStart..end];
    }
}
