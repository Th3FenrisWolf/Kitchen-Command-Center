using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.RegistrationComplete;

public class RegistrationCompletePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    AccountPageQueries accountPages,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        if (CurrentPage is not RegistrationCompletePage page)
        {
            return NotFound();
        }

        var viewModel = new RegistrationCompleteViewModel { LoginUrl = accountPages.GetUrls().Login };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/RegistrationComplete/Index.cshtml", viewModel);
    }
}
