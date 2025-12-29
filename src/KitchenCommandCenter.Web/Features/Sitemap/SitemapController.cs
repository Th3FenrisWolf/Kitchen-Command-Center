using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using KitchenCommandCenter.Web.Features.Cache;
using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;

namespace KitchenCommandCenter.Web.Features.Sitemap;

public class SitemapController(
    IWebsiteChannelContext websiteChannelContext,
    ICacheService cacheService
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
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(config =>
                config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .OfReusableSchema(INavigation_Metadata.REUSABLE_FIELD_SCHEMA_NAME)
            )
            .Parameters(globalParams =>
                globalParams.Where(query =>
                    query.WhereTrue(nameof(INavigation_Metadata.IncludeInNavigation))
                )
            );
        var menuItems = await cacheService.Get<IWebPageFieldsSource>(
            query,
            [nameof(SitemapController), nameof(GetWebPagesAsync)],
            (_) => [$"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|all"]
        );

        return menuItems;
    }
}
