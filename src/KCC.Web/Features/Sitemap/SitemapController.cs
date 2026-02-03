using CMS.Websites;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;

namespace KCC.Web.Features.Sitemap;

public class SitemapController(
    IContentRetriever contentRetriever
) : Controller
{
    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var pages = await GetWebPagesAsync();

        var nodes = pages
            .Select(page => new SitemapNode(page.SystemFields.WebPageUrlPath))
            .ToList();

        return new SitemapProvider().CreateSitemap(
            new SitemapModel([new SitemapNode("/"), .. nodes])
        );
    }

    private async Task<IEnumerable<IWebPageFieldsSource>> GetWebPagesAsync()
    {
        // TODO: Add additional cache dependencies
        // (_) => [$"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|all"]
        var pages = await contentRetriever.RetrievePagesOfReusableSchemas<IWebPageFieldsSource>(
            [INavigation_Metadata.REUSABLE_FIELD_SCHEMA_NAME],
            new(),
            query => query.Where(where =>
                where.WhereTrue(nameof(INavigation_Metadata.IncludeInNavigation))
            ),
            new($"{nameof(SitemapController)}|{nameof(GetWebPagesAsync)}")
        );

        return pages;
    }
}
