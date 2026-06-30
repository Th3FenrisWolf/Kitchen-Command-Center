using System.Security.Claims;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KCC.UnitTests.Features.Api;

public class VariantCookedApiControllerTests
{
    private static UserManager<KCCApplicationUser> MockUserManager(KCCApplicationUser user)
    {
        var store = new Mock<IUserStore<KCCApplicationUser>>();
        var mgr = new Mock<UserManager<KCCApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _ = mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        return mgr.Object;
    }

    [Test]
    public async Task Post_MarksWithResolvedRecipeGuid_AndReturnsCount()
    {
        var cooked = new Mock<IVariantCookedInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var recipeGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        _ = resolver.Setup(r => r.ResolveRecipeGuidAsync(variantGuid, It.IsAny<CancellationToken>())).ReturnsAsync(recipeGuid);
        _ = cooked.Setup(c => c.GetCookedCountForVariant(variantGuid)).Returns(3);
        var controller = new VariantCookedApiController(cooked.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.MarkCooked(variantGuid, default);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        cooked.Verify(c => c.MarkCooked(variantGuid, recipeGuid, memberGuid), Times.Once);
    }

    [Test]
    public async Task Delete_Unmarks()
    {
        var cooked = new Mock<IVariantCookedInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        var controller = new VariantCookedApiController(cooked.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.UnmarkCooked(variantGuid);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        cooked.Verify(c => c.UnmarkCooked(variantGuid, memberGuid), Times.Once);
    }
}
