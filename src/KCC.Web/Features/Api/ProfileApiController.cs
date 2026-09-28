using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Security;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/profile")]
[AutoValidateAntiforgeryToken]
public class ProfileApiController(
    IMemberManager memberManager,
    IMemberService memberService,
    SignInManager<MemberIdentityUser> signInManager,
    IMemberWriteLock memberWriteLock,
    IResourceStringProvider resourceStrings) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.NameRequiredError")]));
        }

        var saved = await memberWriteLock.RunAsync(() =>
        {
            var member = memberService.GetById(signedIn.Key);
            if (member is null)
            {
                return Task.FromResult(false);
            }

            member.SetValue("firstName", request.FirstName.Trim());
            member.SetValue("lastName", request.LastName.Trim());
            memberService.Save(member);
            return Task.FromResult(true);
        });

        return saved ? Ok(new ProfileResponse(true, null)) : Unauthorized();
    }

    [HttpPost("password")]
    [EnableRateLimiting(RateLimits.Account)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.PasswordRequiredError")]));
        }

        var result = await memberWriteLock.RunAsync(async () =>
        {
            var changed = await memberManager.ChangePasswordAsync(signedIn, request.CurrentPassword, request.NewPassword);
            if (changed.Succeeded)
            {
                // The change replaces the member's security stamp, which would end this session at its next check.
                await signInManager.RefreshSignInAsync(signedIn);
            }

            return changed;
        });

        return result.Succeeded
            ? Ok(new ProfileResponse(true, null))
            : Ok(new ProfileResponse(false, [.. result.Errors.Select(error => error.Description)]));
    }
}

public sealed record UpdateProfileRequest(string FirstName, string LastName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ProfileResponse(bool Success, string[] Errors);
