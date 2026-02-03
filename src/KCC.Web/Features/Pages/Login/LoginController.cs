using CMS.Core;
using KCC.Web.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace KCC.Web.Features.Pages.Login;

[Route("Account")]
public class LoginController(
    SignInManager<KCCApplicationUser> signInManager,
    UserManager<KCCApplicationUser> userManager,
    IEventLogService eventLogService
) : Controller
{
    private string[] PasswordErrorCodes =>
    [
        nameof(IdentityErrorDescriber.PasswordTooShort),
        nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        nameof(IdentityErrorDescriber.PasswordRequiresUpper),
        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
    ];

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
    public async Task<IActionResult> Login(LoginViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Features/Pages/Login/Index.cshtml", viewModel);
        }

        var signInResult = SignInResult.Failed;
        try
        {
            signInResult = await signInManager.PasswordSignInAsync(viewModel.UserName, viewModel.Password, viewModel.RememberMe, lockoutOnFailure: false);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(LoginController), nameof(Login), ex);
        }

        if (signInResult.Succeeded)
        {
            return Redirect(viewModel.ReturnUrl ?? "/");
        }

        ModelState.AddModelError(string.Empty, "We couldn't sign you in using the provided credentials.");

        return View("~/Features/Pages/Login/Index.cshtml", viewModel);
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }

        return View("~/Features/Pages/Login/Index.cshtml", new RegisterViewModel());
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Features/Pages/Login/Index.cshtml", viewModel);
        }

        var user = new KCCApplicationUser
        {
            UserName = viewModel.UserName,
            Email = viewModel.Email,
        };

        var registerResult = new IdentityResult();
        try
        {
            registerResult = await userManager.CreateAsync(user, viewModel.Password);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(LoginController), "Register", ex);
            ModelState.AddModelError(string.Empty, "Registration failed.");
        }

        if (registerResult.Succeeded)
        {
            // var newUser = await userManager.FindByIdAsync(user.Id.ToString());
            // return RedirectToAction(nameof(VerificationController.Verification), nameof(Verification), new { user = newUser.MemberGuid });
            return Redirect("/");
        }

        foreach (var error in registerResult.Errors)
        {
            if (PasswordErrorCodes.Contains(error.Code))
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            else
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View("~/Features/Pages/Login/Index.cshtml", viewModel);
    }
}
