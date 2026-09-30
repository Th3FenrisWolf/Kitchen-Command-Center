using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;
using Umbraco.Cms.Core.Web;

namespace KCC.Web.Features.Sitemap;

public class SitemapController(ISitemapPages sitemapPages, IUmbracoContextFactory umbracoContextFactory) : Controller
{
    [HttpGet("sitemap.xml")]
    [HttpHead("sitemap.xml")]
    public IActionResult Index()
    {
        // Umbraco skips creating an UmbracoContext for a request whose path has a file extension
        // (it assumes a client-side asset), so /sitemap.xml has to open one itself before querying content.
        using var contextReference = umbracoContextFactory.EnsureUmbracoContext();

        return new SitemapProvider().CreateSitemap(new SitemapModel(sitemapPages.Urls().Select(url => new SitemapNode(url)).ToList()));
    }
}
