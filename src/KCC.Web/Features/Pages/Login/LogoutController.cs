using KCC.Web.Features.Attributes;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Login;

[LocalizedRoute("account/logout")]
public class LogoutController(SignInManager<KCCApplicationUser> signInManager) : Controller
{
    public async Task<IActionResult> Index(string returnUrl = null)
    {
        await signInManager.SignOutAsync();

        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.HomePage();
        return Redirect(target);
    }
}
