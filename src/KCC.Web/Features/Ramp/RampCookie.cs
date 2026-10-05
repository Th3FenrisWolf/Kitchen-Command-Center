namespace KCC.Web.Features.Ramp;

public static class RampCookie
{
    public const string Name = "kcc-ramp";

    private static readonly object Written = new();

    public static void Write(HttpContext context, string ramp)
    {
        var value = ramp is Ramps.Light or Ramps.Dark ? ramp.ToLowerInvariant() : null;
        context.Items[Written] = value;

        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
        };
        if (value is null)
        {
            context.Response.Cookies.Delete(Name, options);
            return;
        }

        options.MaxAge = TimeSpan.FromDays(365);
        context.Response.Cookies.Append(Name, value, options);
    }

    public static void Clear(HttpContext context) => Write(context, Ramps.Device);

    public static string Read(HttpContext context)
    {
        // The cookie can outlive the sign-in it mirrors, so a visitor follows their device whatever it holds.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // A response's cookie reaches only the next request, so the page that wrote one renders from what it wrote.
        var value = context.Items.TryGetValue(Written, out var written) ? (string)written : context.Request.Cookies[Name];
        return value is "light" or "dark" ? value : null;
    }
}
