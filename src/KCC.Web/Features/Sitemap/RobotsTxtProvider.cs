using System.Text;
using RobotsTxt;

namespace KCC.Web.Features.Sitemap;

public class RobotsTxtProvider(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRobotsTxtProvider
{
    public Task<RobotsTxtResult> GetResultAsync(CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(GetContent()).AsMemory();

        return Task.FromResult(new RobotsTxtResult(buffer, 3600));
    }

    private string GetContent()
    {
        var shouldDenyAll = configuration.GetValue("RobotsTxtDenyAll", true);

        var request = httpContextAccessor.HttpContext.Request;
        var rootDomain = $"{request.Scheme}://{request.Host.Value}";

        var builder = new RobotsTxtOptionsBuilder()
            .AddSitemap($"{rootDomain}/sitemap.xml");

        return (shouldDenyAll ? builder.DenyAll() : builder.AddSection(section => section
            .AddUserAgent("*")
            .Disallow("/admin")
            .Disallow("/error")
        )).Build().ToString();
    }
}
