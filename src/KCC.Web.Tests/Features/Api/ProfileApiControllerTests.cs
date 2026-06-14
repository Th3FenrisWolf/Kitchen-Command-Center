using System.Security.Claims;
using CMS.Core;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KCC.Web.Tests.Features.Api;

public class ProfileApiControllerTests
{
    private static Mock<UserManager<KCCApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<KCCApplicationUser>>();
        return new Mock<UserManager<KCCApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<KCCApplicationUser>> MockSignInManager(UserManager<KCCApplicationUser> userManager) =>
        new(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<KCCApplicationUser>>(),
            null!, null!, null!, null!);

    private static ProfileApiController Build(
        Mock<UserManager<KCCApplicationUser>> userManager,
        Mock<SignInManager<KCCApplicationUser>> signInManager = null)
    {
        var resourceStrings = new Mock<IResourceStringInfoProvider>();
        resourceStrings.Setup(r => r.GetOrDefault(It.IsAny<string>())).Returns((string key) => key);

        signInManager ??= MockSignInManager(userManager.Object);

        return new ProfileApiController(
            userManager.Object,
            signInManager.Object,
            Mock.Of<IEventLogService>(),
            resourceStrings.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) },
            },
        };
    }

    private static ProfileApiController.ProfileResponse Body(IActionResult result) =>
        Assert.IsType<ProfileApiController.ProfileResponse>(Assert.IsType<OkObjectResult>(result).Value);

    [Fact]
    public async Task UpdateProfile_RejectsEmptyName()
    {
        var userManager = MockUserManager();
        var controller = Build(userManager);

        var result = await controller.UpdateProfile(new ProfileApiController.UpdateProfileRequest("", "Carter"));

        Assert.False(Body(result).Success);
        userManager.Verify(m => m.UpdateAsync(It.IsAny<KCCApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_SavesTrimmedNamesAndSucceeds()
    {
        var user = new KCCApplicationUser { FirstName = "Old", LastName = "Name", UserName = "alex" };
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var controller = Build(userManager);

        var result = await controller.UpdateProfile(new ProfileApiController.UpdateProfileRequest("  Alex  ", "  Carter  "));

        Assert.True(Body(result).Success);
        Assert.Equal("Alex", user.FirstName);
        Assert.Equal("Carter", user.LastName);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsIdentityErrors()
    {
        var user = new KCCApplicationUser { UserName = "alex" };
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user))
                   .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));
        var controller = Build(userManager);

        var result = await controller.UpdateProfile(new ProfileApiController.UpdateProfileRequest("Alex", "Carter"));

        var body = Body(result);
        Assert.False(body.Success);
        Assert.Contains("boom", body.Errors);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsUnauthorizedWhenNoCurrentUser()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((KCCApplicationUser)null);
        var controller = Build(userManager);

        var result = await controller.UpdateProfile(new ProfileApiController.UpdateProfileRequest("Alex", "Carter"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_SucceedsAndRefreshesSignIn()
    {
        var user = new KCCApplicationUser { UserName = "alex" };
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.ChangePasswordAsync(user, "old-pass", "new-pass-123"))
                   .ReturnsAsync(IdentityResult.Success);
        var signInManager = MockSignInManager(userManager.Object);
        signInManager.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);
        var controller = Build(userManager, signInManager);

        var result = await controller.ChangePassword(new ProfileApiController.ChangePasswordRequest("old-pass", "new-pass-123"));

        Assert.True(Body(result).Success);
        signInManager.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_ReturnsErrorOnWrongCurrentPassword()
    {
        var user = new KCCApplicationUser { UserName = "alex" };
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.ChangePasswordAsync(user, "wrong", "new-pass-123"))
                   .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));
        var signInManager = MockSignInManager(userManager.Object);
        var controller = Build(userManager, signInManager);

        var result = await controller.ChangePassword(new ProfileApiController.ChangePasswordRequest("wrong", "new-pass-123"));

        var body = Body(result);
        Assert.False(body.Success);
        Assert.Contains("Incorrect password.", body.Errors);
        signInManager.Verify(s => s.RefreshSignInAsync(It.IsAny<KCCApplicationUser>()), Times.Never);
    }
}
