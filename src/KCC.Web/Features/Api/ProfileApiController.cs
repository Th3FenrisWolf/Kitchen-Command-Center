using CMS.Core;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileApiController(
    UserManager<KCCApplicationUser> userManager,
    SignInManager<KCCApplicationUser> signInManager,
    IEventLogService eventLogService,
    IResourceStringInfoProvider resourceStrings
) : ControllerBase
{
    public record UpdateProfileRequest(string FirstName, string LastName);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public record ProfileResponse(bool Success, string[] Errors);

    [HttpPost]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.NameRequiredError")]));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        // Name-only update: never touch Email, so KCCApplicationUser's email-change/account-disable branch can't fire.
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(ProfileApiController), nameof(UpdateProfile), ex);
            return StatusCode(500, new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.UnexpectedError")]));
        }

        return result.Succeeded
            ? Ok(new ProfileResponse(true, null))
            : Ok(new ProfileResponse(false, result.Errors.Select(e => e.Description).ToArray()));
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.PasswordRequiredError")]));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        IdentityResult result;
        try
        {
            result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(ProfileApiController), nameof(ChangePassword), ex);
            return StatusCode(500, new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.UnexpectedError")]));
        }

        if (!result.Succeeded)
        {
            return Ok(new ProfileResponse(false, result.Errors.Select(e => e.Description).ToArray()));
        }

        // Re-issue the auth cookie so the security-stamp change doesn't sign the member out.
        await signInManager.RefreshSignInAsync(user);
        return Ok(new ProfileResponse(true, null));
    }
}
