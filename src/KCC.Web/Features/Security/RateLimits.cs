using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace KCC.Web.Features.Security;

public static class RateLimits
{
    public const string Account = "account";
    public const string Contributions = "contributions";
    public const string Submissions = "submissions";

    // Cloudflare puts the visitor's address in this header, and the tunnel being the only way in is what makes it
    // trustworthy. Without it, in development and tests, the connection's address stands in.
    public static string ClientKey(HttpContext context) =>
        context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() is { Length: > 0 } address
            ? address
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    internal static RateLimitPartition<string> PerClient(HttpContext context, Func<RateLimitOptions, int> permits, TimeSpan window)
    {
        var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits(limits), Window = window, QueueLimit = 0 });
    }
}

public class RateLimitOptions
{
    public int AccountPerMinute { get; set; } = 10;

    public int ContributionsPerMinute { get; set; } = 30;

    public int SubmissionsPerHour { get; set; } = 5;
}
