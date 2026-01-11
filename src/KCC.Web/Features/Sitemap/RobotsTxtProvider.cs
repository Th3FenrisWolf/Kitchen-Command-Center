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
        var request = httpContextAccessor.HttpContext.Request;
        var rootDomain = $"{request.Scheme}://{request.Host.Value}";

        var builder = new RobotsTxtOptionsBuilder();
        builder = builder.AddSitemap($"{rootDomain}/sitemap.xml");

        var shouldDenyAll = configuration.GetValue<bool>("RobotsTxtDenyAll");

        return shouldDenyAll
            ? builder.DenyAll().Build().ToString()
            : builder
                .AddSection(section =>
                    section.AddUserAgent("*").Disallow("/Admin").Disallow("/error")
                )
                .Build()
                .ToString();
    }
}
