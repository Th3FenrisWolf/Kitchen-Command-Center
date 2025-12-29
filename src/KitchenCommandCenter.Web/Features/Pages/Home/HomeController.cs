using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc.Routing;
using KitchenCommandCenter;
using KitchenCommandCenter.Web.Features.Cache;
using KitchenCommandCenter.Web.Features.Pages.Home;
using KitchenCommandCenter.Web.Models.Constants;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    HomePage.CONTENT_TYPE_NAME,
    typeof(HomeController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KitchenCommandCenter.Web.Features.Pages.Home;

public class HomeController(
    IWebsiteChannelContext websiteChannelContext,
    ICacheService cacheService,
    IMapper mapper
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentTypes(config =>
                config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .OfContentType(HomePage.CONTENT_TYPE_NAME)
                    .WithLinkedItems(1)
            )
            .Parameters(param => param.TopN(1));

        var page = (
            await cacheService.Get<HomePage>(query, [nameof(HomeController), nameof(Index)])
        ).FirstOrDefault();

        var viewModel = mapper.Map<HomeViewModel>(page);

        return View("~/Features/Pages/Home/Index.cshtml", viewModel);
    }
}
