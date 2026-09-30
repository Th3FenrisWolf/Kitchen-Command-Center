using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.Login;

public class LoginPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        if (CurrentPage is not LoginPage page)
        {
            return NotFound();
        }

        var localReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (await memberManager.GetCurrentMemberAsync() is not null)
        {
            return Redirect(localReturnUrl ?? "/");
        }

        var viewModel = new LoginViewModel
        {
            ReturnUrl = localReturnUrl,
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

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
        "Login.NewHereDescription");
}
