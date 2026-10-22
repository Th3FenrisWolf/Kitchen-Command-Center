using System.Security.Cryptography;

namespace KCC.Web.Features.Bloom;

public static class VisitClock
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private static readonly object Resolved = new();

    public static Visit Resolve(HttpContext context, TimeProvider clock)
    {
        if (context.Items.TryGetValue(Resolved, out var resolved))
        {
            return (Visit)resolved;
        }

        var now = clock.GetUtcNow();
        var visit = VisitCookie.Parse(context.Request.Cookies[VisitCookie.Name], now) ?? Start(now);
        context.Items[Resolved] = visit;

        if (!context.Response.HasStarted)
        {
            context.Response.Cookies.Append(VisitCookie.Name, VisitCookie.Format(visit), new CookieOptions
            {
                MaxAge = Lifetime,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
            });
        }

        return visit;
    }

    private static Visit Start(DateTimeOffset now) =>
        new(
            BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(sizeof(uint))),
            DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds()),
            now);
}
