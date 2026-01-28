using AutoMapper;
using KCC;
using KCC.Web.Features.Pages.Home;
using KCC.Web.Models.Constants;
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
    IContentRetriever contentRetriever,
    IMapper mapper
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var page = (await contentRetriever.RetrievePages<HomePage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(HomeController)}|{nameof(Index)}")
        )).FirstOrDefault();

        var viewModel = mapper.Map<HomeViewModel>(page);

        return View("~/Features/Pages/Home/Index.cshtml", viewModel);
    }
}
