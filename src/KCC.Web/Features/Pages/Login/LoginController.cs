using CMS.Core;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Login;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

[assembly: RegisterWebPageRoute(
    LoginPage.CONTENT_TYPE_NAME,
    typeof(LoginController),
    ActionName = nameof(LoginController.Login),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Login;

public class LoginController(
    // SignInManager<KCCApplicationUser> signInManager,
    UserManager<KCCApplicationUser> userManager,
    IEventLogService eventLogService,
    IResourceStringInfoProvider resourceStrings
) : Controller
{
    private string[] PasswordErrorCodes =>
    [
        nameof(IdentityErrorDescriber.PasswordTooShort),
        nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        nameof(IdentityErrorDescriber.PasswordRequiresUpper),
        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
    ];

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

    // [HttpPost("Login")]
    // [ValidateAntiForgeryToken]
    // public async Task<IActionResult> Login(LoginViewModel viewModel)
    // {
    //     viewModel.ResourceStrings = LoginStrings;
    //     if (!ModelState.IsValid)
    //     {
    //         return View("~/Features/Pages/Login/Index.cshtml", viewModel);
    //     }
    //     var signInResult = SignInResult.Failed;
    //     try
    //     {
    //         signInResult = await signInManager.PasswordSignInAsync(viewModel.UserName, viewModel.Password, viewModel.RememberMe, lockoutOnFailure: false);
    //     }
    //     catch (Exception ex)
    //     {
    //         eventLogService.LogException(nameof(LoginController), nameof(Login), ex);
    //     }
    //     if (signInResult.Succeeded)
    //     {
    //         return Redirect(viewModel.ReturnUrl ?? Url.HomePage());
    //     }
    //     ModelState.AddModelError(string.Empty, resourceStrings.GetOrDefault("Login.InvalidCredentialsError"));
    //     return View("~/Features/Pages/Login/Index.cshtml", viewModel);
    // }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            return Redirect(Url.HomePage());
        }

        return View("~/Features/Pages/Login/Index.cshtml", new RegisterViewModel
        {
            ResourceStrings = LoginStrings,
        });
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel viewModel)
    {
        viewModel.ResourceStrings = LoginStrings;

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
            ModelState.AddModelError(string.Empty, resourceStrings.GetOrDefault("Login.RegistrationFailedError"));
        }

        if (registerResult.Succeeded)
        {
            // var newUser = await userManager.FindByIdAsync(user.Id.ToString());
            // return RedirectToAction(nameof(VerificationController.Verification), nameof(Verification), new { user = newUser.MemberGuid });
            return Redirect(Url.HomePage());
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
