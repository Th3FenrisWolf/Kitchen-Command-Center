using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Login;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RegistrationCompletePage.CONTENT_TYPE_NAME,
    typeof(RegistrationCompleteController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Login;

public class RegistrationCompleteController(IResourceStringInfoProvider resourceStrings) : Controller
{
    public IActionResult Index()
    {
        var viewModel = new BasePageViewModel
        {
            ResourceStrings = resourceStrings.GetManyOrDefault(
                "RegistrationComplete.Heading",
                "RegistrationComplete.Body",
                "RegistrationComplete.ContinueLink"),
        };

        return View("~/Features/Pages/Login/RegistrationComplete.cshtml", viewModel);
    }
}
