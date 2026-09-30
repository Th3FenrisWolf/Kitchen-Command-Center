using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Security;

namespace KCC.Web.Features.Pages.Account.Logout;

[Route("account/logout")]
[AutoValidateAntiforgeryToken]
public class LogoutController(IMemberSignInManager signInManager, IMemberWriteLock memberWriteLock) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Index(string returnUrl)
    {
        await memberWriteLock.RunAsync(signInManager.SignOutAsync);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
