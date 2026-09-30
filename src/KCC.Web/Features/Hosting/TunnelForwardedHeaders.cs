using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace KCC.Web.Features.Hosting;

public static class TunnelForwardedHeaders
{
    public const string AddressSetting = "Hosting:TunnelAddress";

    public static ForwardedHeadersOptions For(IPAddress tunnel)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };

        // The middleware trusts any sender once both lists are empty, so the tunnel replaces the loopback defaults
        // rather than being added to them.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(tunnel);
        return options;
    }

    // Cloudflare's edge terminates TLS, so every request reaches the app as plain HTTP from the tunnel container.
    // Without the forwarded scheme Umbraco's backoffice sign-in refuses the request, and cookies lose Secure.
    public static WebApplication UseTunnelForwardedHeaders(this WebApplication app)
    {
        var address = app.Configuration[AddressSetting];
        if (!string.IsNullOrWhiteSpace(address))
        {
            app.UseForwardedHeaders(For(IPAddress.Parse(address)));
        }

        return app;
    }
}
