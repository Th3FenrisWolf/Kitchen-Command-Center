using System.Globalization;
using CMS.ContentEngine;
using CMS.Websites;
using KCC.Web.Features.Attributes;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;

namespace KCC.Web.Features.Sitemap;

public class SitemapController(
    IContentRetriever contentRetriever
) : Controller
{
    [LocalizedHttpGet("sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var pages = await GetWebPagesAsync();

        var nodes = pages.Select(page => new SitemapNode(page is HomePage ? "/"
            : page.GetUrl().RelativePath.ToLower(CultureInfo.InvariantCulture))
        ).ToList();

        return new SitemapProvider().CreateSitemap(new(nodes));
    }

    private async Task<IEnumerable<IWebPageFieldsSource>> GetWebPagesAsync()
    {
        var sitemapPages = await contentRetriever.RetrievePagesOfReusableSchemas<IWebPageFieldsSource>(
            [IMetadata.REUSABLE_FIELD_SCHEMA_NAME],
            new(),
            query => query.Where(where => where
                .WhereFalse(nameof(IMetadata.ExcludeFromSitemap))
                .Or()
                .WhereNull(nameof(IMetadata.ExcludeFromSitemap))
            ),
            new($"{nameof(IMetadata.ExcludeFromSitemap)}|{nameof(WhereParameters.WhereFalse)}|Or|{nameof(WhereParameters.WhereNull)}")
        );

        return sitemapPages;
    }
}
