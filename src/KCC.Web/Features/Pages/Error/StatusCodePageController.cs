using KCC.Web.Features.Models.Generated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Error;

public class StatusCodePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        var page = (StatusCodePage)CurrentPage;
        Response.StatusCode = page.StatusCode;

        return View("~/Features/Pages/Error/Index.cshtml", ErrorViewModel.For(page.StatusCode, page.Heading, page.Body?.ToHtmlString()));
    }
}
