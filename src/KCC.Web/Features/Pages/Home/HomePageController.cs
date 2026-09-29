using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Home;

public class HomePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        var page = (HomePage)CurrentPage;
        var viewModel = new HomeViewModel { Sections = HomeSections.From(page.Sections) };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Home/Index.cshtml", viewModel);
    }
}
