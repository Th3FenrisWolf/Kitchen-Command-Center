using KCC;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account.RegistrationComplete;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RegistrationCompletePage.CONTENT_TYPE_NAME,
    typeof(RegistrationCompleteController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account.RegistrationComplete;

public class RegistrationCompleteController(IContentRetriever contentRetriever) : Controller
{
    public async Task<IActionResult> Index()
    {
        var page = await contentRetriever.RetrievePage<RegistrationCompletePage>();

        if (page is null)
        {
            return NotFound();
        }

        var viewModel = new BasePageViewModel();

        await page.MapMetadata(viewModel);
        return View("~/Features/Pages/Account/RegistrationComplete/Index.cshtml", viewModel);
    }
}
