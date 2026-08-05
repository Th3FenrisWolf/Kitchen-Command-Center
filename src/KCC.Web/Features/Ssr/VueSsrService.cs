using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Caching.Memory;
using Polly.CircuitBreaker;

namespace KCC.Web.Features.Ssr;

public class VueSsrService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<VueSsrService> logger)
{
    private bool IsEnabled => configuration.GetValue("VueSsr:Enabled", true);
    private HttpClient HttpClient => httpClientFactory.CreateClient("VueSsr");
    private TimeSpan CacheDuration => TimeSpan.FromSeconds(configuration.GetValue("VueSsr:CacheDurationSeconds", 60));

    public async Task<IHtmlContent> RenderVueSsrAsync(
        IHtmlContent headerContent,
        IHtmlContent bodyContent,
        IHtmlContent footerContent,
        bool isPreview = false,
        CancellationToken cancellationToken = default)
    {
        var encoder = HtmlEncoder.Default;
        var builder = new StringBuilder(8192);
        using var writer = new StringWriter(builder);

        // Render header
        headerContent.WriteTo(writer, encoder);
        var header = builder.ToString();
        builder.Clear();

        // Render body
        bodyContent.WriteTo(writer, encoder);
        var body = builder.ToString();
        builder.Clear();

        // Render footer
        footerContent.WriteTo(writer, encoder);
        var footer = builder.ToString();

        var result = await RenderAsync(header, body, footer, isPreview, cancellationToken);

        return new SsrHtmlContent(result);
    }

    public async Task<SsrResult> RenderAsync(
        string headerContent,
        string bodyContent,
        string footerContent,
        bool isPreview = false,
        CancellationToken cancellationToken = default)
    {
        var baseResult = new SsrResult
        {
            HeaderContent = headerContent,
            BodyContent = bodyContent,
            FooterContent = footerContent,
            IsPreview = isPreview
        };

        if (!IsEnabled)
        {
            return baseResult;
        }

        var cacheKey = GenerateCacheKey(headerContent, bodyContent, footerContent);

        if (cache.TryGetValue(cacheKey, out string cachedHtml) && cachedHtml is not null)
        {
            logger.LogDebug("SSR cache hit for key {CacheKey}", cacheKey[..16]);

            return baseResult with { Html = cachedHtml };
        }

        try
        {
            // Use Activity.Current for distributed tracing, or generate a new ID
            var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/render");
            request.Headers.Add("X-Request-Id", requestId);
            request.Content = JsonContent.Create(new { headerContent, bodyContent, footerContent, isPreview });

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

            // Cache the successful response
            cache.Set(cacheKey, result.Html, CacheDuration);

            return baseResult with { Html = result.Html };
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

    private static string GenerateCacheKey(string header, string body, string footer)
    {
        var combined = $"{header}{body}{footer}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return $"ssr:{Convert.ToHexString(hashBytes)}";
    }
}
