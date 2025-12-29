using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using KitchenCommandCenter.Web.Features.Cache;
using Microsoft.AspNetCore.Mvc;

namespace KitchenCommandCenter.Web.Features.Pages.Error;

[Route("Error")]
public class ErrorController(
    IWebsiteChannelContext websiteChannelContext,
    ICacheService cacheService,
    IMapper mapper
) : Controller
{
    public async Task<IActionResult> Index()
    {
        return await HandleStatusCode(
            HttpContext.Response.StatusCode.ToString(CultureInfo.InvariantCulture)
        );
    }

    [Route("{statusCode}")]
    public async Task<IActionResult> HandleStatusCode(string statusCode)
    {
        var query = new ContentItemQueryBuilder().ForContentType(
            StatusCodePage.CONTENT_TYPE_NAME,
            config =>
                config
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .Where(query =>
                        query.WhereEquals(nameof(StatusCodePage.StatusCode), statusCode)
                    )
                    .TopN(1)
        );

        var page = (
            await cacheService.Get<StatusCodePage>(
                query,
                [nameof(ErrorController), nameof(HandleStatusCode), statusCode]
            )
        ).FirstOrDefault();

        var viewModel = mapper.Map<ErrorViewModel>(page);

        return View("~/Features/Pages/Error/Index.cshtml", viewModel);
    }
}
