using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using KitchenCommandCenter;
using KitchenCommandCenter.Web.Features.Cache;
using KitchenCommandCenter.Web.Features.Pages.PageBuilder;
using KitchenCommandCenter.Web.Models.Constants;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    PageBuilderPage.CONTENT_TYPE_NAME,
    typeof(PageBuilderController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KitchenCommandCenter.Web.Features.Pages.PageBuilder;

public class PageBuilderController(
    IWebsiteChannelContext websiteChannelContext,
    IWebPageDataContextRetriever webPageDataContextRetriever,
    ICacheService cacheService,
    IMapper mapper
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(config =>
                config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .OfContentType(PageBuilderPage.CONTENT_TYPE_NAME)
                    .WithLinkedItems(1)
            )
            .Parameters(param =>
                param
                    .Where(query =>
                        query.WhereEquals(
                            nameof(IWebPageFieldsSource.SystemFields.WebPageItemID),
                            pageId
                        )
                    )
                    .TopN(1)
            );

        var page = (
            await cacheService.Get<PageBuilderPage>(
                query,
                [
                    nameof(PageBuilderController),
                    nameof(Index),
                    pageId.ToString(CultureInfo.InvariantCulture),
                ]
            )
        ).FirstOrDefault();

        var viewModel = mapper.Map<PageBuilderViewModel>(page);

        return View("~/Features/Pages/PageBuilder/Index.cshtml", viewModel);
    }
}
