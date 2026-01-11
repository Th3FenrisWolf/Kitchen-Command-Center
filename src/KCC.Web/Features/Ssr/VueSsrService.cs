using System.Text.Json.Serialization;

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

        try
        {
            var response = await this.httpClient.PostAsJsonAsync(
                "/render",
                new SsrRequest(headerContent, bodyContent, footerContent),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning(
                    "SSR service returned {StatusCode}, falling back to client-side rendering",
                    response.StatusCode);
                return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
            }

            var result = await response.Content.ReadFromJsonAsync<SsrResponse>(cancellationToken);

            if (result?.Html is null)
            {
                this.logger.LogWarning("SSR service returned null HTML, falling back to client-side rendering");
                return SsrResult.ClientSideOnly(headerContent, bodyContent, footerContent);
            }

            this.logger.LogDebug("SSR render completed in {RenderTime}ms", result.RenderTime);

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
