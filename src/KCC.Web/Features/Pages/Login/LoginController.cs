using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Login;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    LoginPage.CONTENT_TYPE_NAME,
    typeof(LoginController),
    ActionName = nameof(LoginController.Login),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Login;

public class LoginController(IResourceStringInfoProvider resourceStrings) : Controller
{
    private Dictionary<string, string> LoginStrings => resourceStrings.GetManyOrDefault(
        "Login.SignIn",
        "Login.SignUp",
        "Login.UsernamePlaceholder",
        "Login.EmailPlaceholder",
        "Login.PasswordPlaceholder",
        "Login.ConfirmPasswordPlaceholder",
        "Login.RememberMe",
        "Login.HaveAccount",
        "Login.HaveAccountDescription",
        "Login.NewHere",
        "Login.NewHereDescription"
    );

    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated && !HttpContext.IsPreview())
        {
            return Redirect(returnUrl ?? Url.HomePage());
        }

        return View("~/Features/Pages/Login/Index.cshtml", new LoginViewModel
        {
            ReturnUrl = returnUrl,
            ResourceStrings = LoginStrings,
        });
    }
}
