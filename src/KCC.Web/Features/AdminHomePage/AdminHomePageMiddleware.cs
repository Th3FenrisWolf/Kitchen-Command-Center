using System.Reflection;
using System.Text;

namespace KCC.Web.Features.AdminHomePage;

/// <summary>
/// Wires up the middleware that makes the custom KCC Home application Kentico's
/// effective admin landing page. Two stages:
/// <list type="number">
///   <item>A 302 redirect from <c>/admin</c> and <c>/admin/</c> to <c>/admin/home</c>.</item>
///   <item>An HTML response interceptor for any <c>/admin/*</c> request that injects
///   a script before <c>&lt;/body&gt;</c>. The script intercepts anchor clicks and
///   <c>history.pushState/replaceState</c> calls targeting <c>/admin</c> so the
///   admin SPA's home button (which navigates client-side and never hits the
///   server) ends up on our Home regardless of which page the user was on.</item>
/// </list>
/// Both stages must be registered between authentication and Kentico's own
/// middleware (<c>app.UseKentico()</c>).
/// </summary>
public static class AdminHomePageMiddleware
{
    private const string TargetPath = "/admin/home";

    private const string ScriptResourceName = "KCC.Web.Features.AdminHomePage.admin-home-redirect.js";

    private static readonly Lazy<string> RedirectScript = new(LoadRedirectScript);

    public static IApplicationBuilder UseAdminHomePageRedirect(this IApplicationBuilder app)
    {
        app.UseAdminLandingRedirect();
        app.UseAdminHomePageScriptInjection();
        return app;
    }

    private static string LoadRedirectScript()
    {
        var assembly = typeof(AdminHomePageMiddleware).Assembly;
        using var stream = assembly.GetManifestResourceStream(ScriptResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ScriptResourceName}' not found. Verify it is registered as <EmbeddedResource> in KCC.Web.csproj.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return $"<script>{reader.ReadToEnd()}</script>";
    }

    private static void UseAdminLandingRedirect(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value.TrimEnd('/');
            if (string.Equals(path, "/admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(TargetPath, permanent: false);
                return;
            }

            await next();
        });
    }

    private static void UseAdminHomePageScriptInjection(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/admin"))
            {
                await next();
                return;
            }

            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next();

                context.Response.Body = originalBody;

                var contentType = context.Response.ContentType;
                var isHtml = contentType is not null
                    && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);

                buffer.Position = 0;

                if (isHtml && context.Response.StatusCode == 200)
                {
                    var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                    const string marker = "</body>";
                    var idx = html.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var modified = html.Insert(idx, RedirectScript.Value);
                        var bytes = Encoding.UTF8.GetBytes(modified);
                        context.Response.ContentLength = bytes.Length;
                        await context.Response.Body.WriteAsync(bytes);
                        return;
                    }
                }

                await buffer.CopyToAsync(context.Response.Body);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        });
    }
}
