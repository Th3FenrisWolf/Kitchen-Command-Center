using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace KCC.Web.Features.Security;

public class SecurityComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // ASP.NET Core never marks the antiforgery cookie Secure by default, and with Always it refuses to issue a
        // token over plain HTTP, which the E2E site serves. Following the request's scheme, as Umbraco's member cookie
        // does, marks it Secure over the tunnel's HTTPS.
        builder.Services.Configure<AntiforgeryOptions>(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

        builder.Services.Configure<RateLimitOptions>(builder.Config.GetSection("RateLimits"));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many attempts. Try again later." },
                    cancellationToken));
            };
            options.AddPolicy(RateLimits.Account, context => RateLimits.PerClient(context, limits => limits.AccountPerMinute, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimits.Contributions, context => RateLimits.PerClient(context, limits => limits.ContributionsPerMinute, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimits.Submissions, context => RateLimits.PerClient(context, limits => limits.SubmissionsPerHour, TimeSpan.FromHours(1)));
        });

        // After routing, so the limiter can read which policy the endpoint asks for.
        builder.Services.Configure<UmbracoPipelineOptions>(options => options.AddFilter(new UmbracoPipelineFilter("KCC rate limits")
        {
            PostRouting = app => app.UseRateLimiter(),
        }));
    }
}
