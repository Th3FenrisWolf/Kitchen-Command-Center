using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KCC.Web.Features.Ssr;

/// <summary>
/// Service for rendering Vue components on the server using the Node.js SSR sidecar.
/// </summary>
public class VueSsrService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<VueSsrService> logger;
    private readonly bool isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="VueSsrService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public VueSsrService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VueSsrService> logger)
    {
        this.httpClient = httpClientFactory.CreateClient("VueSsr");
        this.logger = logger;
        this.isEnabled = configuration.GetValue("VueSsr:Enabled", true);
    }

    /// <summary>
    /// Renders Vue components to HTML using the SSR service.
    /// Falls back to client-side rendering if SSR fails.
    /// </summary>
    /// <param name="serverContent">The server-rendered page content (from Razor).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The fully rendered HTML including Vue components.</returns>
    public async Task<SsrResult> RenderAsync(string serverContent, CancellationToken cancellationToken = default)
    {
        if (!this.isEnabled)
        {
            return SsrResult.ClientSideOnly(serverContent);
        }

        try
        {
            var response = await this.httpClient.PostAsJsonAsync(
                "/render",
                new SsrRequest(serverContent),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning(
                    "SSR service returned {StatusCode}, falling back to client-side rendering",
                    response.StatusCode);
                return SsrResult.ClientSideOnly(serverContent);
            }

            var result = await response.Content.ReadFromJsonAsync<SsrResponse>(cancellationToken);

            if (result?.Html is null)
            {
                this.logger.LogWarning("SSR service returned null HTML, falling back to client-side rendering");
                return SsrResult.ClientSideOnly(serverContent);
            }

            this.logger.LogDebug("SSR render completed in {RenderTime}ms", result.RenderTime);

            return new SsrResult(
                Html: result.Html,
                WasServerRendered: true,
                ServerContent: serverContent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogDebug("SSR request was cancelled");
            return SsrResult.ClientSideOnly(serverContent);
        }
        catch (TaskCanceledException)
        {
            this.logger.LogWarning("SSR service timed out, falling back to client-side rendering");
            return SsrResult.ClientSideOnly(serverContent);
        }
        catch (HttpRequestException ex)
        {
            this.logger.LogWarning(ex, "SSR service unavailable, falling back to client-side rendering");
            return SsrResult.ClientSideOnly(serverContent);
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
}

/// <summary>
/// Request payload for SSR rendering.
/// </summary>
/// <param name="ServerContent">The page content to embed in the Vue app.</param>
public record SsrRequest(
    [property: JsonPropertyName("serverContent")] string ServerContent);

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
/// <param name="ServerContent">The original server content for client-side fallback.</param>
public record SsrResult(string Html, bool WasServerRendered, string ServerContent)
{
    /// <summary>
    /// Creates a result for client-side only rendering.
    /// </summary>
    /// <param name="serverContent">The server content to pass to the client.</param>
    /// <returns>An SSR result configured for client-side rendering.</returns>
    public static SsrResult ClientSideOnly(string serverContent) =>
        new(Html: string.Empty, WasServerRendered: false, ServerContent: serverContent);
}
