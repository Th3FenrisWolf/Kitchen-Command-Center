using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Bloom;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Caching.Memory;
using Polly.CircuitBreaker;

namespace KCC.Web.Features.Ssr;

public class VueSsrService(
    IHttpClientFactory httpClientFactory,
    SsrRenderCache cache,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<VueSsrService> logger)
{
    private bool IsEnabled => configuration.GetValue("VueSsr:Enabled", true);
    private HttpClient HttpClient => httpClientFactory.CreateClient("VueSsr");
    private TimeSpan CacheDuration => TimeSpan.FromSeconds(configuration.GetValue("VueSsr:CacheDurationSeconds", 60));

    public async Task<SsrHtmlContent> RenderVueSsrAsync(
        IHtmlContent headerContent,
        IHtmlContent bodyContent,
        IHtmlContent footerContent,
        BloomInputs bloom,
        bool isPreview = false,
        CancellationToken cancellationToken = default) =>
        new SsrHtmlContent(await RenderAsync(Html(headerContent), Html(bodyContent), Html(footerContent), bloom, isPreview, cancellationToken));

    public async Task<SsrResult> RenderAsync(
        string headerContent,
        string bodyContent,
        string footerContent,
        BloomInputs bloom,
        bool isPreview = false,
        CancellationToken cancellationToken = default)
    {
        var baseResult = new SsrResult
        {
            HeaderContent = headerContent,
            BodyContent = bodyContent,
            FooterContent = footerContent,
            IsPreview = isPreview,
            Bloom = bloom,
        };

        if (!IsEnabled)
        {
            return baseResult;
        }

        var cacheKey = GenerateCacheKey(headerContent, bodyContent, footerContent, bloom, isPreview);

        if (cache.Entries.TryGetValue(cacheKey, out CachedRender cached) && cached is not null)
        {
            logger.LogDebug("SSR cache hit for key {CacheKey}", cacheKey[..16]);

            return baseResult with { Html = cached.Html, Css = cached.Css, Schedule = cached.Schedule };
        }

        try
        {
            var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/render");
            request.Headers.Add("X-Request-Id", requestId);
            request.Content = JsonContent.Create(new { headerContent, bodyContent, footerContent, isPreview, bloom = new { seed = bloom.Seed, page = bloom.Page } });

            var response = await HttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "SSR service returned {StatusCode} for request {RequestId}, falling back to client-side rendering",
                    response.StatusCode,
                    requestId);

                // SsrHtmlContent re-emits the error into the page so the dev error
                // overlay can surface render failures that the silent client-side
                // fallback would otherwise hide.
                if (environment.IsDevelopment()
                    && await TryReadErrorAsync(response, cancellationToken) is { Error: not null } error)
                {
                    return baseResult with { ErrorMessage = error.Error, ErrorStack = error.Stack };
                }

                return baseResult;
            }

            var result = await response.Content.ReadFromJsonAsync<SsrResponse>(cancellationToken);

            if (result is null || result.Html is null)
            {
                logger.LogWarning(
                    "SSR service returned null response for request {RequestId}, falling back to client-side rendering",
                    requestId);

                return baseResult;
            }

            logger.LogDebug(
                "SSR render completed in {RenderTime}ms for request {RequestId}",
                result.RenderTime,
                requestId);

            var render = new CachedRender(result.Html, result.Css, result.Schedule);
            cache.Entries.Set(
                cacheKey,
                render,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration, Size = render.Size });

            return baseResult with { Html = result.Html, Css = result.Css, Schedule = result.Schedule };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("SSR request was cancelled");
            return baseResult;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("SSR service timed out, falling back to client-side rendering");
            return baseResult;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "SSR service unavailable, falling back to client-side rendering");
            return baseResult;
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("SSR circuit breaker is open, falling back to client-side rendering");
            return baseResult;
        }
    }

    private static string Html(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static async Task<SsrErrorResponse> TryReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<SsrErrorResponse>(cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static string GenerateCacheKey(string header, string body, string footer, BloomInputs bloom, bool isPreview)
    {
        var combined = $"{(isPreview ? "preview" : "live")}\n{bloom.Seed}\n{bloom.Page}\n{header}{body}{footer}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return $"ssr:{Convert.ToHexString(hashBytes)}";
    }

    private sealed record CachedRender(string Html, string Css, string Schedule)
    {
        public long Size => Html.Length + (Css?.Length ?? 0) + (Schedule?.Length ?? 0);
    }
}

public sealed class SsrRenderCache(long sizeLimit)
{
    public MemoryCache Entries { get; } = new(new MemoryCacheOptions { SizeLimit = sizeLimit });
}
