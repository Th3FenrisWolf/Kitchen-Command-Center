using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace KCC.Web.Features.Ssr;

/// <summary>
/// Service for rendering Vue components on the server using the Node.js SSR sidecar.
/// </summary>
public class VueSsrService
{
    private readonly HttpClient httpClient;
    private readonly IMemoryCache cache;
    private readonly ILogger<VueSsrService> logger;
    private readonly bool isEnabled;
    private readonly TimeSpan cacheDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="VueSsrService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="cache">Memory cache for SSR responses.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public VueSsrService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<VueSsrService> logger)
    {
        this.httpClient = httpClientFactory.CreateClient("VueSsr");
        this.cache = cache;
        this.logger = logger;
        this.isEnabled = configuration.GetValue("VueSsr:Enabled", true);
        this.cacheDuration = TimeSpan.FromSeconds(configuration.GetValue("VueSsr:CacheDurationSeconds", 60));
    }

    /// <summary>
    /// Renders Vue components to HTML using the SSR service.
    /// Falls back to client-side rendering if SSR fails.
    /// </summary>
    /// <param name="headerContent">The header content from ViewComponent.</param>
    /// <param name="bodyContent">The main page body content.</param>
    /// <param name="footerContent">The footer content from ViewComponent.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The fully rendered HTML including Vue components.</returns>
    public async Task<SsrResult> RenderAsync(
        string headerContent,
        string bodyContent,
        string footerContent,
        CancellationToken cancellationToken = default)
    {
        if (!this.isEnabled)
        {
            return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
        }

        // Generate cache key from content hash
        var cacheKey = GenerateCacheKey(headerContent, bodyContent, footerContent);

        // Check cache first
        if (this.cache.TryGetValue(cacheKey, out string cachedHtml) && cachedHtml is not null)
        {
            this.logger.LogDebug("SSR cache hit for key {CacheKey}", cacheKey[..16]);
            return new SsrResult(
                Html: cachedHtml,
                WasServerRendered: true,
                HeaderContent: headerContent,
                BodyContent: bodyContent,
                FooterContent: footerContent);
        }

        try
        {
            // Use Activity.Current for distributed tracing, or generate a new ID
            var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/render");
            request.Headers.Add("X-Request-Id", requestId);
            request.Content = JsonContent.Create(new SsrRequest(headerContent, bodyContent, footerContent));

            var response = await this.httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning(
                    "SSR service returned {StatusCode} for request {RequestId}, falling back to client-side rendering",
                    response.StatusCode,
                    requestId);
                return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
            }

            var result = await response.Content.ReadFromJsonAsync<SsrResponse>(cancellationToken);

            if (result is null || result.Html is null)
            {
                this.logger.LogWarning(
                    "SSR service returned null response for request {RequestId}, falling back to client-side rendering",
                    requestId);
                return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
            }

            this.logger.LogDebug(
                "SSR render completed in {RenderTime}ms for request {RequestId}",
                result.RenderTime,
                requestId);

            // Cache the successful response
            this.cache.Set(cacheKey, result.Html, this.cacheDuration);

            return new SsrResult(
                Html: result.Html,
                WasServerRendered: true,
                HeaderContent: headerContent,
                BodyContent: bodyContent,
                FooterContent: footerContent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogDebug("SSR request was cancelled");
            return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
        }
        catch (TaskCanceledException)
        {
            this.logger.LogWarning("SSR service timed out, falling back to client-side rendering");
            return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, "SSR service unavailable, falling back to client-side rendering");
            return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
        }
    }

    /// <summary>
    /// Checks if the SSR service is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>True if the service is reachable and healthy.</returns>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a short timeout for health checks
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await this.httpClient.GetAsync("/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a cache key from the content hash.
    /// </summary>
    private static string GenerateCacheKey(string header, string body, string footer)
    {
        var combined = $"{header}{body}{footer}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return $"ssr:{Convert.ToHexString(hashBytes)}";
    }
}

/// <summary>
/// Request payload for SSR rendering.
/// </summary>
/// <param name="HeaderContent">The header content from ViewComponent.</param>
/// <param name="BodyContent">The main page body content.</param>
/// <param name="FooterContent">The footer content from ViewComponent.</param>
public record SsrRequest(
    [property: JsonPropertyName("headerContent")] string HeaderContent,
    [property: JsonPropertyName("bodyContent")] string BodyContent,
    [property: JsonPropertyName("footerContent")] string FooterContent);

/// <summary>
/// Response from the SSR service.
/// </summary>
/// <param name="Html">The rendered HTML.</param>
/// <param name="RenderTime">Time taken to render in milliseconds.</param>
public record SsrResponse(
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("renderTime")] int RenderTime);

/// <summary>
/// Result of an SSR render operation.
/// </summary>
/// <param name="Html">The HTML to render (either SSR output or fallback).</param>
/// <param name="WasServerRendered">Whether the content was server-rendered.</param>
/// <param name="HeaderContent">The header content for client-side fallback.</param>
/// <param name="BodyContent">The body content for client-side fallback.</param>
/// <param name="FooterContent">The footer content for client-side fallback.</param>
public record SsrResult(
    string Html,
    bool WasServerRendered,
    string HeaderContent,
    string BodyContent,
    string FooterContent)
{
    /// <summary>
    /// Creates a result for client-side only rendering.
    /// </summary>
    /// <param name="headerContent">The header content to pass to the client.</param>
    /// <param name="bodyContent">The body content to pass to the client.</param>
    /// <param name="footerContent">The footer content to pass to the client.</param>
    /// <returns>An SSR result configured for client-side rendering.</returns>
    public static SsrResult ClientSideOnly(string headerContent, string bodyContent, string footerContent) =>
        new(
            Html: string.Empty,
            WasServerRendered: false,
            HeaderContent: headerContent,
            BodyContent: bodyContent,
            FooterContent: footerContent);
}
