using CMS.Websites;
using KCC;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.PageBuilder;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    PageBuilderPage.CONTENT_TYPE_NAME,
    typeof(PageBuilderController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.PageBuilder;

public class PageBuilderController(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IContentRetriever contentRetriever
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

        var page = (await contentRetriever.RetrievePages<PageBuilderPage>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId)
                )
                .TopN(1),
            new($"{nameof(PageBuilderController)}|{nameof(Index)}|{pageId}")
        )).FirstOrDefault();

        var viewModel = new PageBuilderViewModel();

        await page.MapMetadata(viewModel);
        return View("~/Features/Pages/PageBuilder/Index.cshtml", viewModel);
    }
}
