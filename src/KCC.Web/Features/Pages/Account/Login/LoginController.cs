using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account.Login;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    LoginPage.CONTENT_TYPE_NAME,
    typeof(LoginController),
    ActionName = nameof(LoginController.Login),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account.Login;

public class LoginController(
    IContentRetriever contentRetriever,
    IResourceStringInfoProvider resourceStrings
) : Controller
{
    public async Task<IActionResult> Login(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated && !HttpContext.IsAdmin())
        {
            return Redirect(returnUrl ?? Url.HomePage());
        }

        var page = await contentRetriever.RetrievePage<LoginPage>();

        if (page is null)
        {
            return NotFound();
        }

        var viewModel = new LoginViewModel()
        {
            ReturnUrl = returnUrl,
            ResourceStrings = GetStrings(),
        };

        await page.MapMetadata(viewModel);
        return View("~/Features/Pages/Account/Login/Index.cshtml", viewModel);
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
