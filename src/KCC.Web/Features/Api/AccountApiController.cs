using CMS.Core;
using CMS.Websites;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/account")]
public class AccountApiController(
    SignInManager<KCCApplicationUser> signInManager,
    UserManager<KCCApplicationUser> userManager,
    IEventLogService eventLogService,
    IResourceStringInfoProvider resourceStrings,
    IContentRetriever contentRetriever
) : ControllerBase
{
    public record LoginRequest(string UserName, string Password, bool RememberMe, string ReturnUrl);
    public record RegisterRequest(string UserName, string Email, string Password);
    public record AuthResponse(bool Success, string[] Errors, string RedirectUrl);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username and password are required."], null));
        }

        var result = SignInResult.Failed;
        try
        {
            result = await signInManager.PasswordSignInAsync(
                request.UserName, request.Password, request.RememberMe, lockoutOnFailure: false);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(AccountApiController), nameof(Login), ex);
            return StatusCode(500, new AuthResponse(false, ["An error occurred during sign in."], null));
        }

        if (result.Succeeded)
        {
            return Ok(new AuthResponse(true, null, SafeReturnUrl(request.ReturnUrl)));
        }

        var error = result switch
        {
            { IsLockedOut: true } => resourceStrings.GetOrDefault("Login.LockedOutError"),
            { IsNotAllowed: true } => resourceStrings.GetOrDefault("Login.NotAllowedError"),
            { RequiresTwoFactor: true } => resourceStrings.GetOrDefault("Login.TwoFactorRequiredError"),
            _ => resourceStrings.GetOrDefault("Login.InvalidCredentialsError"),
        };
        return Ok(new AuthResponse(false, [error], null));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new AuthResponse(true, null, Url.HomePage()));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username, email and password are required."], null));
        }

        var user = new KCCApplicationUser { UserName = request.UserName, Email = request.Email };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, request.Password);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(AccountApiController), nameof(Register), ex);
            return StatusCode(500, new AuthResponse(false, ["An error occurred during registration."], null));
        }

        if (!result.Succeeded)
        {
            return Ok(new AuthResponse(false, [..result.Errors.Select(e => e.Description)], null));
        }

        // RequireConfirmedAccount = true — don't auto-sign-in; send user to the registration-complete page
        var registrationCompletePage = await contentRetriever.RetrieveFirstPage<RegistrationCompletePage>();
        return Ok(new AuthResponse(true, null, registrationCompletePage?.GetUrl().RelativePath));
    }

    private string SafeReturnUrl(string returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl : Url.HomePage();
}
