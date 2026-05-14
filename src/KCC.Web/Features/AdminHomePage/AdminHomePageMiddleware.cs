using System.Text;
using CMS.Core;
using Vite.AspNetCore;

namespace KCC.Web.Features.AdminHomePage;

/// <summary>
/// Wires up the middleware that makes the custom KCC Home application Kentico's
/// effective admin landing page. Two stages:
/// <list type="number">
///   <item>A 302 redirect from <c>/admin</c> and <c>/admin/</c> to <c>/admin/home</c>.</item>
///   <item>An HTML response interceptor for any <c>/admin/*</c> request that injects
///   a script tag before <c>&lt;/body&gt;</c>. The script intercepts anchor clicks and
///   <c>history.pushState/replaceState</c> calls targeting <c>/admin</c> so the
///   admin SPA's home button (which navigates client-side and never hits the
///   server) ends up on our Home regardless of which page the user was on.</item>
/// </list>
/// </summary>
public static class AdminHomePageMiddleware
{
    internal const string ScriptSourceKey = "Features/AdminHomePage/admin-home-redirect.ts";

    private const string TargetPath = "/admin/home";

    public static IApplicationBuilder UseAdminHomePageRedirect(this IApplicationBuilder app)
    {
        app.UseAdminLandingRedirect();
        app.UseAdminHomePageScriptInjection();
        return app;
    }

    internal static string BuildScriptTag()
    {
        var devServer = Service.Resolve<IViteDevServerStatus>();
        if (devServer.IsEnabled)
        {
            var basePath = string.IsNullOrEmpty(devServer.BasePath)
                ? string.Empty
                : devServer.BasePath.TrimEnd('/');

            return $"<script type=\"module\" src=\"{basePath}/{ScriptSourceKey}\"></script>";
        }

        var manifest = Service.Resolve<IViteManifest>();
        var chunk = manifest[ScriptSourceKey] ?? throw new InvalidOperationException(
            $"Vite manifest is missing entry '{ScriptSourceKey}'. Ensure 'yarn build' has run and the entry is registered in vite.config.ts."
        );

        return $"<script type=\"module\" src=\"/{chunk.File}\"></script>";
    }

    private static void UseAdminLandingRedirect(this IApplicationBuilder app) => app.Use(
        async (context, next) =>
    {
        if (context.Request.Path.Value?.TrimEnd('/').Equals("/admin", StringComparison.OrdinalIgnoreCase) is true)
        {
            context.Response.Redirect(TargetPath, permanent: false);
            return;
        }

        await next();
    });

    private static void UseAdminHomePageScriptInjection(this IApplicationBuilder app) => app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/admin"),
        branch => branch.Use(async (context, next) =>
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next();

            context.Response.Body = originalBody;

            var contentType = context.Response.ContentType;
            var isHtml = contentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase);

            buffer.Position = 0;

            if (isHtml is true && context.Response.StatusCode is 200)
            {
                var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                const string marker = "</body>";
                var index = html.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    var modified = html.Insert(index, BuildScriptTag());
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
    }));
}
