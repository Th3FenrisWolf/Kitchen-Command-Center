using System.Globalization;
using CMS.ContentEngine;
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
            .Select(page =>
                page is HomePage
                    ? new SitemapNode("/")
                    : new SitemapNode(page.GetUrl().RelativePath.ToLower(CultureInfo.InvariantCulture)))
            .ToList();

        return new SitemapProvider().CreateSitemap(
            new SitemapModel(nodes)
        );
    }

    private async Task<IEnumerable<IWebPageFieldsSource>> GetWebPagesAsync()
    {
        var sitemapPages = await contentRetriever.RetrievePagesOfReusableSchemas<IWebPageFieldsSource>(
            [IMetadata.REUSABLE_FIELD_SCHEMA_NAME],
            new(),
            query => query.Where(where =>
                where
                    .WhereFalse(nameof(IMetadata.ExcludeFromSitemap))
                    .Or()
                    .WhereNull(nameof(IMetadata.ExcludeFromSitemap))
            ),
            new RetrievalCacheSettings($"{nameof(IMetadata.ExcludeFromSitemap)}|{nameof(WhereParameters.WhereFalse)}|Or|{nameof(WhereParameters.WhereNull)}")
        );

        return sitemapPages;
    }
}
