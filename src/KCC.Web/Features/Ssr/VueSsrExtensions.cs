using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Vite.AspNetCore;

namespace KCC.Web.Features.Ssr;

public static class VueSsrExtensions
{
    public static IServiceCollection AddVueSsr(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<VueSsrService>();

        services.AddViteServices(options =>
        {
            options.Server.AutoRun = true;
            options.Server.PackageManager = "yarn";
            options.Server.ScriptName = "dev:all";
        });

        services.AddHttpClient("VueSsr", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.BaseAddress = new Uri(configuration["VueSsr:BaseUrl"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    // Registers the SSR hydration middleware and, in development, the Vite dev
    // server. Must run after UseKentico so PageBuilderMode is available to the
    // PreviewJsonUrlSyncMiddleware.
    public static IApplicationBuilder UseVueSsr(this WebApplication app)
    {
        app.UseMiddleware<PreviewJsonUrlSyncMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebSockets();

            if (bool.Parse(app.Configuration["ASPNETCORE_VITE"]))
            {
                app.UseViteDevelopmentServer(true);
            }
        }

        return app;
    }

    // Circuit breaker: break after 5 failures for 30 seconds
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30)
            );

    // Retry policy: retry 2 times with exponential backoff + jitter to prevent thundering herd
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds((100 * Math.Pow(2, retryAttempt)) + Random.Shared.Next(-20, 20))
            );
}
