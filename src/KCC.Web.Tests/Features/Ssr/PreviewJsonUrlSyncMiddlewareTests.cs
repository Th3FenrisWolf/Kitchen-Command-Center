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
            // Simulate the inner pipeline setting a stale Content-Length
            // based on the pre-rewrite buffer. Ours is empty, so 0 is fine
            // as a stand-in — what matters is that the middleware clears it.
            ctx.Response.ContentLength = 42;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);

        // Fix assertion: middleware MUST clear the stale Content-Length
        // before writing the rewritten body. Without the fix the middleware
        // either leaves it at 42 or overwrites to bytes.Length (0). The
        // guard + null-clear path produces null.
        Assert.Null(httpContext.Response.ContentLength);
    }

    [Fact]
    public async Task InvokeAsync_HeadersAlreadyStarted_DoesNotTouchContentLength()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/cmsctx/pm/x/~/home";
        httpContext.Response.ContentType = "text/html";
        httpContext.Response.Body = new MemoryStream();

        // Install a feature that reports HasStarted=true so the middleware's
        // guard short-circuits the ContentLength assignment.
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
        // The guard leaves the stale value alone because HasStarted=true.
        // This proves the guard is in place; removing the `if (!HasStarted)`
        // would clear to null and fail this assertion.
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
            // Simulate the inner pipeline writing some bytes to the response
            // (which land in the middleware's buffered stream), then failing.
            // The outer middleware's inner catch must ensure those bytes still
            // reach originalBody so the client sees what the inner pipeline
            // had produced before the exception.
            await ctx.Response.Body.WriteAsync(partialPayload);
            throw new InvalidOperationException("boom");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(httpContext));

        // Body restored.
        Assert.Same(originalBody, httpContext.Response.Body);

        // Buffered bytes flushed. Without the inner catch, originalBody is empty
        // because the `await next(context)` throws before we reach the post-next
        // flush code — distinguishes the fix from the bug.
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

        // Payload contains a server-content script block with a raw URL that the
        // rewrite path WOULD decorate if it ran on this response. The HTML before
        // the JSON blob provides the decoration source so BuildDecorationMap has
        // something to match.
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

        // Must be byte-for-byte identical to the original payload. Without the
        // StatusCode >= 400 guard, SyncServerContentUrls would rewrite `\u0022~/home\u0022`
        // to `\u0022/cmsctx/pm/abc/~/home?uh=1234\u0022`, producing a different string.
        Assert.Equal(payload, written);
    }

    // Minimal IHttpResponseFeature that reports HasStarted=true. Shares the
    // parent context's header dictionary so ContentType/Headers reads still
    // return the values set on DefaultHttpContext.
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
        // "~/" would partial-match inside "~/recipes". Boundary + longest-first
        // ordering should prevent that, so "~/recipes" gets the recipes URL.
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

public class ServerContentCouplingTests
{
    [Fact]
    public void SsrHtmlContent_WriteTo_EmitsSharedScriptTagConstant()
    {
        // Coupling guard: renders an SsrHtmlContent via IHtmlContent and asserts
        // the rendered HTML contains the EXACT string the middleware looks for.
        // If SsrHtmlContent.WriteTo ever emits something different from
        // ServerContentScriptOpen (e.g., hard-coded literal drifts), this test
        // catches it. Also confirms the middleware pass-through is a no-op on
        // SSR output that has no preview URLs.
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
