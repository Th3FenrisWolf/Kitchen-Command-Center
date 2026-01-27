using KCC;
using KCC.Web.Features.Cache;
using KCC.Web.Features.Sitemap;
using KCC.Web.Features.Ssr;
using Kentico.Activities.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Polly;
using Polly.Extensions.Http;
using RobotsTxt;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICacheService, CacheService>();
builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();
builder.Services.AddViteServices(options =>
{
    options.Server.AutoRun = true;
    options.Server.PackageManager = "yarn";
    options.Server.ScriptName = "dev:all";
});

// Vue SSR service with resilience policies
var ssrBaseUrl = builder.Configuration["VueSsr:BaseUrl"] ?? "http://localhost:3001";

builder.Services.AddHttpClient("VueSsr", client =>
{
    client.BaseAddress = new Uri(ssrBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddScoped<VueSsrService>();

// Circuit breaker: break after 5 failures for 30 seconds
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));

// Retry policy: retry 2 times with exponential backoff + jitter to prevent thundering herd
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromMilliseconds((100 * Math.Pow(2, retryAttempt)) + Random.Shared.Next(-20, 20)));

builder.Services.AddAutoMapper(typeof(Program));

// Enable desired Kentico Xperience features
builder.Services.AddKentico(features =>
{
    features.UsePageBuilder(
        new PageBuilderOptions
        {
            RegisterDefaultSection = false,
            DefaultSectionIdentifier = "KCC.BaseSection",
            ContentTypeNames = [HomePage.CONTENT_TYPE_NAME, PageBuilderPage.CONTENT_TYPE_NAME],
        }
    );
    features.UseWebPageRouting();
    features.UseActivityTracking();

    // features.UseEmailStatisticsLogging();
    // features.UseEmailMarketing();
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKenticoMiniProfiler();
}

builder.Services.AddAuthentication();

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.InitKentico();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

app.UseRobotsTxt();

if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
    app.UseWebSockets();

    if (app.Configuration["ASPNETCORE_VITE"]?.ToLower() == "true")
    {
        app.UseViteDevelopmentServer(true);
    }
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.Kentico().MapRoutes();

app.MapControllerRoute(name: "error", pattern: "error/{0}");

app.Run();
