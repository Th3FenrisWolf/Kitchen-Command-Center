using AutoMapper;
using CMS.Core;
using KCC.Web.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace KCC.Web.Features.Pages.Login;

[Route("Account")]
public class LoginController(
    SignInManager<KCCApplicationUser> signInManager,
    IEventLogService eventLogService
) : Controller
{
    [HttpGet("Login")]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect(returnUrl ?? "/");
        }

        return View("~/Features/Pages/Login/Index.cshtml", new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var viewModel = new LoginViewModel { ReturnUrl = model.ReturnUrl };
        MergeLoginModels(viewModel, model);

        if (!ModelState.IsValid)
        {
            return View("~/Features/Pages/Login/Index.cshtml", viewModel);
        }

        var signInResult = SignInResult.Failed;
        try
        {
            signInResult = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(LoginController), nameof(Login), ex);
        }

        if (signInResult.Succeeded)
        {
            return Redirect(model.ReturnUrl ?? "/");
        }

        ModelState.AddModelError(string.Empty, "We couldn't sign you in using the provided credentials.");

        return View("~/Features/Pages/Login/Index.cshtml", viewModel);
    }

    private static void MergeLoginModels(LoginViewModel pageModel, LoginViewModel formModel)
    {
        pageModel.Email = formModel.Email;
        pageModel.Password = formModel.Password;
        pageModel.RememberMe = formModel.RememberMe;
    }
}
