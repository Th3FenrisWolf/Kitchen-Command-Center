using KCC;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account.RegistrationComplete;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RegistrationCompletePage.CONTENT_TYPE_NAME,
    typeof(RegistrationCompleteController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account.RegistrationComplete;

public class RegistrationCompleteController() : Controller
{
    public IActionResult Index() => View(
        "~/Features/Pages/Account/RegistrationComplete/Index.cshtml",
        new BasePageViewModel()
    );
}
