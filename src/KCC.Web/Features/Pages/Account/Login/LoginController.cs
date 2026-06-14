using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account.Login;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    LoginPage.CONTENT_TYPE_NAME,
    typeof(LoginController),
    ActionName = nameof(LoginController.Login),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account.Login;

public class LoginController(IResourceStringInfoProvider resourceStrings) : Controller
{
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated && !HttpContext.IsAdmin())
        {
            return Redirect(returnUrl ?? Url.HomePage());
        }

        return View("~/Features/Pages/Account/Login/Index.cshtml", new LoginViewModel
        {
            ReturnUrl = returnUrl,
            ResourceStrings = GetStrings(),
        });
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
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
}
