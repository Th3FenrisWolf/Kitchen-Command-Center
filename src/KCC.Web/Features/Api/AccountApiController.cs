using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Security;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/account")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Account)]
public class AccountApiController(
    IMemberSignInManager signInManager,
    IMemberManager memberManager,
    IMemberWriteLock memberWriteLock,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username and password are required."], null));
        }

        var result = await memberWriteLock.RunAsync(() =>
            signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: true));
        if (result.Succeeded)
        {
            return Ok(new AuthResponse(true, null, Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : "/"));
        }

        var error = result switch
        {
            { IsLockedOut: true } => resourceStrings.GetOrDefault("Login.LockedOutError"),
            { IsNotAllowed: true } => resourceStrings.GetOrDefault("Login.NotAllowedError"),
            _ => resourceStrings.GetOrDefault("Login.InvalidCredentialsError"),
        };
        return Ok(new AuthResponse(false, [error], null));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username, email and password are required."], null));
        }

        // Every new member waits for the owner's approval before they can sign in.
        var userName = request.UserName.Trim();
        var member = MemberIdentityUser.CreateNew(userName, request.Email.Trim(), Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
        var result = await memberWriteLock.RunAsync(() => memberManager.CreateAsync(member, request.Password));

        return result.Succeeded
            ? Ok(new AuthResponse(true, null, accountPages.GetUrls().RegistrationComplete))
            : Ok(new AuthResponse(false, [.. result.Errors.Select(error => error.Description)], null));
    }
}

public sealed record LoginRequest(string UserName, string Password, bool RememberMe, string ReturnUrl);

public sealed record RegisterRequest(string UserName, string Email, string Password);

public sealed record AuthResponse(bool Success, string[] Errors, string RedirectUrl);
