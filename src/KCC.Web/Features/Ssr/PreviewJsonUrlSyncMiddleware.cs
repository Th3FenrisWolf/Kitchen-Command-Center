using System.Text;
using System.Text.RegularExpressions;
using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Ssr;

// Kentico's virtual-context decorator wraps <img src> / <a href> URLs with a
// /cmsctx/pm/<guid>/.../h/<hash>/~/ prefix in preview mode, but it does not
// touch URLs inside <script type="application/json"> blobs. The Vue SSR payload
// embeds raw URLs in that JSON, so client hydration sees raw URLs and compares
// against the decorated URLs already in the DOM — every affected <img>, <a>,
// and <source> emits a hydration attribute mismatch warning.
//
// This middleware scans the final response HTML for URLs Kentico already
// decorated, builds a raw -> decorated map, and rewrites matching raw URLs
// inside the <script id="server-content"> JSON so both sides of hydration see
// the same value. We never synthesize a decorated URL ourselves — the per-URL
// hash depends on Kentico internals — we only propagate decorations Kentico
// produced.
public partial class PreviewJsonUrlSyncMiddleware(RequestDelegate next)
{
    private const string PreviewPathMarker = "/cmsctx/";
    private const string ScriptCloseTag = "</script>";
    private const string ServerContentScriptTag = SsrHtmlContent.ServerContentScriptOpen;

    public async Task InvokeAsync(HttpContext context)
    {
        // Gate on the request path rather than Kentico's mode/preview context.
        // This middleware must be registered BEFORE UseKentico() so that in the
        // response phase it runs AFTER Kentico's virtual-context decorator has
        // already rewritten <img src> and <a href> URLs. At that point in the
        // pipeline Kentico's PageBuilder context is not yet available, so we
        // use the URL prefix as the gate instead.
        var path = context.Request.Path.Value;
        if (path is null || !path.Contains(PreviewPathMarker, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            var contentType = context.Response.ContentType;
            var isHtml = contentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase);

            buffer.Position = 0;

            if (isHtml is true && context.Response.StatusCode is 200)
            {
                var encoding = ResolveEncoding(context.Response);
                var html = await new StreamReader(buffer, encoding).ReadToEndAsync();
                var rewritten = SyncServerContentUrls(html);
                var bytes = encoding.GetBytes(rewritten);

                if (!context.Response.HasStarted)
                {
                    context.Response.ContentLength = null;
                }

                await originalBody.WriteAsync(bytes, context.RequestAborted);

                return;
            }

            await buffer.CopyToAsync(originalBody);
        }
        catch
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    internal static string SyncServerContentUrls(string html)
    {
        var scriptStart = html.IndexOf(ServerContentScriptTag, StringComparison.Ordinal);
        if (scriptStart < 0)
        {
            return html;
        }

        var jsonStart = scriptStart + ServerContentScriptTag.Length;
        var scriptEnd = html.IndexOf(ScriptCloseTag, jsonStart, StringComparison.Ordinal);
        if (scriptEnd < 0)
        {
            return html;
        }

        // Scan only the rendered HTML portion (everything before the JSON blob)
        // so we don't pick up any URL we later inject into the JSON.
        var decorationMap = BuildDecorationMap(html, 0, scriptStart);
        if (decorationMap.Count == 0)
        {
            return html;
        }

        var jsonContent = html.Substring(jsonStart, scriptEnd - jsonStart);
        var updatedJson = ApplyDecorations(jsonContent, decorationMap);
        if (updatedJson == jsonContent)
        {
            return html;
        }

        return string.Concat(html.AsSpan(0, jsonStart), updatedJson, html.AsSpan(scriptEnd));
    }

    internal static Dictionary<string, string> BuildDecorationMap(string html, int start, int end)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var length = end - start;
        if (length <= 0)
        {
            return map;
        }

        var scanned = html.Substring(start, length);

        foreach (var match in DecoratedUrlRegex().Matches(scanned).ToList())
        {
            // In HTML attributes Kentico emits `&amp;uh=<hash>`; the JSON blob
            // carries plain `&`. Normalize so both the map key (for JSON lookup)
            // and the map value (what we write back into JSON) use plain `&`.
            var decorated = match.Value.Replace("&amp;", "&", StringComparison.Ordinal);
            if (!TryExtractRawPath(decorated, out var rawPath))
            {
                continue;
            }

            // The JSON blob carries URLs in two forms: Razor-literal values keep
            // the app-relative `~/path` form; Kentico-emitted values (content
            // assets, routed URLs) come through as absolute `/path`. Register
            // both so ApplyDecorations finds whichever form the JSON actually
            // uses.
            map.TryAdd("~/" + rawPath, decorated);
            map.TryAdd("/" + rawPath, decorated);
        }

        return map;
    }

    // Given a decorated URL, recover the path Kentico started from (everything
    // after the virtual-context marker, with the `uh=<hash>` token stripped).
    //   /cmsctx/pm/<...>/~/<path>?<q>&uh=<hash>  ->  <path>?<q>
    //   /cmsctx/pm/<...>/~/<path>?uh=<hash>      ->  <path>
    //   /cmsctx/pm/<...>/-/?uh=<hash>            ->  (empty)
    //   /cmsctx/pm/<...>/-/uh=<hash>             ->  (empty)
    internal static bool TryExtractRawPath(string decorated, out string rawPath)
    {
        var markerMatch = VirtualContextMarkerRegex().Match(decorated);
        if (!markerMatch.Success)
        {
            rawPath = string.Empty;
            return false;
        }

        var afterMarker = decorated[(markerMatch.Index + markerMatch.Length)..];

        // Kentico appends uh= as &uh=, ?uh=, or directly after the marker
        // (no separator) for root/empty-path URLs.
        var ampUh = afterMarker.IndexOf("&uh=", StringComparison.Ordinal);
        var queryUh = afterMarker.IndexOf("?uh=", StringComparison.Ordinal);
        var bareUh = afterMarker.StartsWith("uh=", StringComparison.Ordinal) ? 0 : -1;
        var uhIdx = (ampUh, queryUh, bareUh) switch
        {
            (>= 0, >= 0, >= 0) => Math.Min(ampUh, Math.Min(queryUh, bareUh)),
            (>= 0, >= 0, _) => Math.Min(ampUh, queryUh),
            (>= 0, _, >= 0) => Math.Min(ampUh, bareUh),
            (_, >= 0, >= 0) => Math.Min(queryUh, bareUh),
            (>= 0, _, _) => ampUh,
            (_, >= 0, _) => queryUh,
            (_, _, >= 0) => bareUh,
            _ => -1,
        };

        rawPath = uhIdx >= 0 ? afterMarker[..uhIdx] : afterMarker;
        return true;
    }

    internal static string ApplyDecorations(string jsonContent, Dictionary<string, string> map)
    {
        if (map.Count == 0)
        {
            return jsonContent;
        }

        // Longer keys first: .NET regex alternation matches left-to-right and
        // takes the first successful branch, so we must list specific keys
        // (e.g. ~/products) before their prefixes (e.g. ~/).
        var ordered = map.OrderByDescending(kvp => kvp.Key.Length).ToList();
        var alternatives = string.Join("|", ordered.Select(kvp => Regex.Escape(kvp.Key)));

        // Only replace when the raw URL is the full value of a JSON string,
        // bounded by one of the delimiters that URLs land between on their way
        // to the browser:
        //   \u0022       - SsrHtmlContent's default encoder form of " around
        //                  URL string values inside the server-content JSON.
        //   &quot;       - Vue.Prop's HTML-encoded " around URL values inside
        //                  HTML attributes (Vue.Prop uses UnsafeRelaxedJsonEscaping
        //                  to keep the raw JSON, then HTML-encodes " to &quot;).
        //   \u0026quot;  - Default-encoder form of &quot;, produced when a
        //                  Vue.Prop-rendered fragment (containing &quot;) is
        //                  re-serialized through SsrHtmlContent as part of
        //                  HeaderContent / BodyContent / FooterContent.
        // The relaxed-encoder \" form never appears in either emission path,
        // so it's not in the boundary set.
        const string boundary = @"(?:&quot;|\\u0022|\\u0026quot;)";
        var pattern = $"({boundary})({alternatives})({boundary})";
        var regex = new Regex(pattern, RegexOptions.Compiled);

        return regex.Replace(jsonContent, m =>
        {
            var key = m.Groups[2].Value;
            return m.Groups[1].Value + map[key] + m.Groups[3].Value;
        });
    }

    private static Encoding ResolveEncoding(HttpResponse response)
    {
        var contentType = response.ContentType;
        if (string.IsNullOrEmpty(contentType))
        {
            return Encoding.UTF8;
        }

        try
        {
            var parsed = new System.Net.Mime.ContentType(contentType);
            if (!string.IsNullOrEmpty(parsed.CharSet))
            {
                return Encoding.GetEncoding(parsed.CharSet);
            }
        }
        catch (FormatException)
        {
            // Malformed Content-Type; fall through to UTF-8.
        }
        catch (ArgumentException)
        {
            // Unknown charset; fall through to UTF-8.
        }

        return Encoding.UTF8;
    }

    // Kentico's path separator between the virtual-context prefix and the raw
    // URL. Accept both `/~/` (documented) and `/-/` (observed in some builds).
    [GeneratedRegex(@"/cmsctx/pm/(?:[^""'\s>]+/)+[~\-]/[^""'\s>]+", RegexOptions.Compiled)]
    private static partial Regex DecoratedUrlRegex();

    [GeneratedRegex(@"/[~\-]/", RegexOptions.Compiled)]
    private static partial Regex VirtualContextMarkerRegex();
}
