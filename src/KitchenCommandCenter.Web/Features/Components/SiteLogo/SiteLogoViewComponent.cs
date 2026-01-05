using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using KitchenCommandCenter.Web.Features.Cache;
using Microsoft.AspNetCore.Mvc;

namespace KitchenCommandCenter.Web.Features.Components.SiteLogo;

public class SiteLogoViewComponent(
    IWebsiteChannelContext websiteChannelContext,
    ICacheService cacheService
) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var query = new ContentItemQueryBuilder().ForContentTypes(config =>
            config
                .ForWebsite(websiteChannelContext.WebsiteChannelName)
                .OfContentType(SiteSettings.CONTENT_TYPE_NAME)
                .WithLinkedItems(1)
        );

        var siteSettings = (
            await cacheService.Get<SiteSettings>(
                query,
                [nameof(SiteLogoViewComponent), nameof(InvokeAsync)]
            )
        ).FirstOrDefault();

        var viewModel = new SiteLogoViewModel
        {
            Logo = siteSettings.SiteLogo.FirstOrDefault()?.Asset.Url,
        };

        return View("~/Features/Components/SiteLogo/Default.cshtml", viewModel);
    }
}
