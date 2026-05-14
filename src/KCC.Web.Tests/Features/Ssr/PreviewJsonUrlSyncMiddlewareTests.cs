using KCC.Web.Features.Ssr;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Text;
using System.Text.Encodings.Web;
using Xunit;

namespace KCC.Web.Tests.Features.Ssr;

public class PreviewJsonUrlSyncMiddlewareTests
{
    [Fact]
    public void SyncServerContentUrls_NoServerContentScript_ReturnsHtmlUnchanged()
    {
        var html = "<html><body>no script</body></html>";

        var result = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);

        Assert.Equal(html, result);
    }
}

public class InvokeAsyncTests
{
    [Fact]
    public async Task InvokeAsync_HeadersNotStarted_ClearsStaleContentLength()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/cmsctx/pm/x/~/home";
        httpContext.Response.ContentType = "text/html";
        httpContext.Response.Body = new MemoryStream();

        var middleware = new PreviewJsonUrlSyncMiddleware(ctx =>
        {
            ctx.Response.ContentLength = 42;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);

        Assert.Null(httpContext.Response.ContentLength);
    }

    [Fact]
    public async Task InvokeAsync_HeadersAlreadyStarted_DoesNotTouchContentLength()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/cmsctx/pm/x/~/home";
        httpContext.Response.ContentType = "text/html";
        httpContext.Response.Body = new MemoryStream();

        var startedFeature = new StartedResponseFeature(httpContext.Response.Headers);
        httpContext.Features.Set<IHttpResponseFeature>(startedFeature);

        httpContext.Response.ContentLength = 42;

        var called = false;
        var middleware = new PreviewJsonUrlSyncMiddleware(ctx =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);

        Assert.True(called);
        Assert.Equal(42, httpContext.Response.ContentLength);
    }

    [Fact]
    public async Task InvokeAsync_InnerDelegateThrowsAfterWritingBytes_FlushesBufferedBytesToClient()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/cmsctx/pm/x/~/home";
        var originalBody = new MemoryStream();
        httpContext.Response.Body = originalBody;

        var partialPayload = Encoding.UTF8.GetBytes("partial error page body\n");

        var middleware = new PreviewJsonUrlSyncMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(partialPayload);
            throw new InvalidOperationException("boom");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));

        Assert.Same(originalBody, httpContext.Response.Body);

        originalBody.Position = 0;
        var flushed = originalBody.ToArray();
        Assert.Equal(partialPayload, flushed);
    }

    [Fact]
    public async Task InvokeAsync_NonSuccessStatus_PassesBufferedBodyThroughWithoutRewrite()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/cmsctx/pm/x/~/home";
        httpContext.Response.ContentType = "text/html";
        var originalBody = new MemoryStream();
        httpContext.Response.Body = originalBody;

        var payload = "<html><body><a href=\"/cmsctx/pm/abc/~/home?uh=1234\">home</a></body>\n" +
                      "<script id=\"server-content\" type=\"application/json\">{\"bodyContent\":\"\\u0022~/home\\u0022\"}</script>\n" +
                      "</html>\n";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var middleware = new PreviewJsonUrlSyncMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = 500;
            await ctx.Response.Body.WriteAsync(payloadBytes);
        });

        await middleware.InvokeAsync(httpContext);

        originalBody.Position = 0;
        var written = Encoding.UTF8.GetString(originalBody.ToArray());

        Assert.Equal(payload, written);
    }

    [Fact]
    public async Task InvokeAsync_NonCmsctxPath_SkipsBuffering()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/home";
        httpContext.Response.ContentType = "text/html";
        var originalBody = new MemoryStream();
        httpContext.Response.Body = originalBody;

        var called = false;
        var middleware = new PreviewJsonUrlSyncMiddleware(ctx =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);

        Assert.True(called);
        Assert.Same(originalBody, httpContext.Response.Body);
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public StartedResponseFeature(IHeaderDictionary headers)
        {
            Headers = headers;
        }

        public int StatusCode { get; set; } = 200;
        public string ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; }
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}

public class ApplyDecorationsTests
{
    [Fact]
    public void ApplyDecorations_ReplacesAllMatchingKeys()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["~/logo.png"] = "/cmsctx/pm/abc/~/logo.png",
            ["/home"] = "/cmsctx/pm/abc/~/home",
        };
        var json = @"{""a"":""\u0022~/logo.png\u0022"",""b"":""\u0022/home\u0022""}";

        var result = PreviewJsonUrlSyncMiddleware.ApplyDecorations(json, map);

        Assert.Contains("/cmsctx/pm/abc/~/logo.png", result);
        Assert.Contains("/cmsctx/pm/abc/~/home", result);
    }

    [Fact]
    public void ApplyDecorations_LongerKeyWinsOverPrefixKey()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["~/"] = "/cmsctx/pm/abc/~/root",
            ["~/recipes"] = "/cmsctx/pm/abc/~/recipes",
        };
        var json = @"{""r"":""\u0022~/recipes\u0022""}";

        var result = PreviewJsonUrlSyncMiddleware.ApplyDecorations(json, map);

        Assert.Contains("/cmsctx/pm/abc/~/recipes", result);
        Assert.DoesNotContain("/cmsctx/pm/abc/~/rootrecipes", result);
    }

    [Fact]
    public void ApplyDecorations_EmptyMap_ReturnsInputUnchanged()
    {
        var result = PreviewJsonUrlSyncMiddleware.ApplyDecorations(
            "json", new Dictionary<string, string>(StringComparer.Ordinal));

        Assert.Equal("json", result);
    }
}

public class EndToEndSyncTests
{
    private const string CmsCtxPrefix =
        "/cmsctx/pm/641b5bce-8072-4bcd-8e48-9d7178b826b7/lang/en" +
        "/wpid/1/c1d/1/c1d/1055/readonly/0/pbmode/edit/h" +
        "/233c3e3e4c5c9ec3f2d16d6c8289efdbe8ff7213caaa83c4e9bf902a507295";

    [Fact]
    public void SyncServerContentUrls_RewritesHomeUrl_WithTildeMarker()
    {
        var decoratedHome = CmsCtxPrefix + "/~/?uh=abc123";
        var html = BuildHtml(
            ssrBody: "<a href=\"" + decoratedHome + "\">Home</a>",
            headerContent: "<AppHeader home-url=\"~/\" />");

        var result = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);

        Assert.Contains(decoratedHome, result);
    }

    [Fact]
    public void SyncServerContentUrls_RewritesHomeUrl_WithDashMarker()
    {
        var decoratedHome = CmsCtxPrefix + "/-/?uh=abc123";
        var html = BuildHtml(
            ssrBody: "<a href=\"" + decoratedHome + "\">Home</a>",
            headerContent: "<AppHeader home-url=\"~/\" />");

        var result = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);

        Assert.Contains(decoratedHome, result);
    }

    [Fact]
    public void SyncServerContentUrls_RewritesContentAssetUrl_InVueProp()
    {
        var rawAssetUrl = "~/getContentAsset/f67d70e7-a891/3571d6da/KCC-Bone.webp?language=en";
        var decoratedAsset = CmsCtxPrefix +
            "/~/getContentAsset/f67d70e7-a891/3571d6da/KCC-Bone.webp?language=en&amp;uh=def456";

        var logoJson = "{\"asset\":{\"url\":\"" + rawAssetUrl + "\"}}";
        var logoPropHtml = logoJson
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;");
        var headerContent = "<AppHeader :logo=\"" + logoPropHtml + "\" />";

        var html = BuildHtml(
            ssrBody: "<img src=\"" + decoratedAsset + "\" />",
            headerContent: headerContent);

        var result = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);

        var jsonStart = result.IndexOf(SsrHtmlContent.ServerContentScriptOpen, StringComparison.Ordinal)
            + SsrHtmlContent.ServerContentScriptOpen.Length;
        var jsonEnd = result.IndexOf("</script>", jsonStart, StringComparison.Ordinal);
        var jsonPortion = result[jsonStart..jsonEnd];

        Assert.Contains(CmsCtxPrefix + "/~/getContentAsset/f67d70e7-a891", jsonPortion);
    }

    [Fact]
    public void SyncServerContentUrls_RewritesNavItemUrl_InVueProp()
    {
        var decoratedRecipes = CmsCtxPrefix + "/~/recipes?uh=ghi789";
        var navItemsJson = "[{\"displayText\":\"Recipes\",\"url\":\"~/recipes\"}]";
        var navPropHtml = navItemsJson
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;");
        var headerContent = "<AppHeader :main-nav-items=\"" + navPropHtml + "\" />";

        var html = BuildHtml(
            ssrBody: "<a href=\"" + decoratedRecipes + "\">Recipes</a>",
            headerContent: headerContent);

        var result = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);

        Assert.Contains(CmsCtxPrefix + "/~/recipes", result);
    }

    [Fact]
    public void BuildDecorationMap_ExtractsRawPath_WhenUhDirectlyAfterMarker()
    {
        var html = "<a href=\"" + CmsCtxPrefix + "/-/uh=abc123\">Home</a>";

        var map = PreviewJsonUrlSyncMiddleware.BuildDecorationMap(html, 0, html.Length);

        Assert.True(map.ContainsKey("~/"), "Map should contain '~/' key for root URL");
        Assert.True(map.ContainsKey("/"), "Map should contain '/' key for root URL");
    }

    [Fact]
    public void TryExtractRawPath_HandlesUhDirectlyAfterMarker()
    {
        var decorated = CmsCtxPrefix + "/-/uh=abc123";

        var result = PreviewJsonUrlSyncMiddleware.TryExtractRawPath(decorated, out var rawPath);

        Assert.True(result);
        Assert.Equal(string.Empty, rawPath);
    }

    [Fact]
    public void TryExtractRawPath_HandlesQuestionMarkUh()
    {
        var decorated = CmsCtxPrefix + "/~/?uh=abc123";

        var result = PreviewJsonUrlSyncMiddleware.TryExtractRawPath(decorated, out var rawPath);

        Assert.True(result);
        Assert.Equal(string.Empty, rawPath);
    }

    [Fact]
    public void TryExtractRawPath_HandlesAmpersandUh()
    {
        var decorated = CmsCtxPrefix + "/~/getContentAsset/img.webp?language=en&uh=abc123";

        var result = PreviewJsonUrlSyncMiddleware.TryExtractRawPath(decorated, out var rawPath);

        Assert.True(result);
        Assert.Equal("getContentAsset/img.webp?language=en", rawPath);
    }

    [Fact]
    public void TryExtractRawPath_NoUh_ReturnsFullPathAfterMarker()
    {
        var decorated = CmsCtxPrefix + "/~/getContentAsset/img.webp?language=en";

        var result = PreviewJsonUrlSyncMiddleware.TryExtractRawPath(decorated, out var rawPath);

        Assert.True(result);
        Assert.Equal("getContentAsset/img.webp?language=en", rawPath);
    }

    private static string BuildHtml(string ssrBody, string headerContent)
    {
        var result = new SsrResult
        {
            Html = ssrBody,
            HeaderContent = headerContent,
            BodyContent = "",
            FooterContent = "",
        };
        var content = new SsrHtmlContent(result);
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}

public class ServerContentCouplingTests
{
    [Fact]
    public void SsrHtmlContent_WriteTo_EmitsSharedScriptTagConstant()
    {
        var result = new SsrResult
        {
            Html = "<div>body</div>",
            HeaderContent = "<header></header>",
            BodyContent = "<main></main>",
            FooterContent = "<footer></footer>",
        };
        var content = new SsrHtmlContent(result);
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);

        var html = writer.ToString();

        Assert.Contains(SsrHtmlContent.ServerContentScriptOpen, html, StringComparison.Ordinal);

        var rewritten = PreviewJsonUrlSyncMiddleware.SyncServerContentUrls(html);
        Assert.Equal(html, rewritten);
    }
}
