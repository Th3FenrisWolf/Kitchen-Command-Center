using KCC;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Home;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    HomePage.CONTENT_TYPE_NAME,
    typeof(HomeController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Home;

public class HomeController(
    IContentRetriever contentRetriever
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var page = (await contentRetriever.RetrievePages<HomePage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(HomeController)}|{nameof(Index)}")
        )).FirstOrDefault();

        var viewModel = new HomeViewModel();
        page.MapMetadata(viewModel);
        page.MapWebPageFields(viewModel);

        return View("~/Features/Pages/Home/Index.cshtml", viewModel);
    }
}
