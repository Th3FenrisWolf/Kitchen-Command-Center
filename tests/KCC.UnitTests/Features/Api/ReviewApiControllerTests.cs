using System.Security.Claims;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KCC.UnitTests.Features.Api;

public class ReviewApiControllerTests
{
    private static UserManager<KCCApplicationUser> MockUserManager(KCCApplicationUser user)
    {
        var store = new Mock<IUserStore<KCCApplicationUser>>();
        var mgr = new Mock<UserManager<KCCApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _ = mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        return mgr.Object;
    }

    [Test]
    public async Task Put_RejectsOutOfRangeRating()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var user = new KCCApplicationUser { MemberGuid = Guid.NewGuid() };
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(user));

        var result = await controller.UpsertReview(Guid.NewGuid(), new ReviewApiController.ReviewRequest(9m, "x"), default);

        _ = await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
        reviews.Verify(r => r.Upsert(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Put_ReturnsNotFoundWhenVariantUnknown()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        _ = resolver.Setup(r => r.ResolveRecipeGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        var user = new KCCApplicationUser { MemberGuid = Guid.NewGuid() };
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(user));

        var result = await controller.UpsertReview(Guid.NewGuid(), new ReviewApiController.ReviewRequest(5m, "ok"), default);

        _ = await Assert.That(result).IsTypeOf<NotFoundObjectResult>();
    }

    [Test]
    public async Task Put_UpsertsWithResolvedRecipeGuid()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var recipeGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        _ = resolver.Setup(r => r.ResolveRecipeGuidAsync(variantGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipeGuid);
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.UpsertReview(variantGuid, new ReviewApiController.ReviewRequest(4m, "tasty"), default);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        reviews.Verify(r => r.Upsert(variantGuid, recipeGuid, memberGuid, 4m, "tasty"), Times.Once);
    }

    [Test]
    public async Task Put_AcceptsHalfStarRating()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var recipeGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        _ = resolver.Setup(r => r.ResolveRecipeGuidAsync(variantGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipeGuid);
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.UpsertReview(variantGuid, new ReviewApiController.ReviewRequest(3.5m, "half"), default);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        reviews.Verify(r => r.Upsert(variantGuid, recipeGuid, memberGuid, 3.5m, "half"), Times.Once);
    }

    [Test]
    public async Task Put_RejectsNonHalfStepRating()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var user = new KCCApplicationUser { MemberGuid = Guid.NewGuid() };
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(user));

        var result = await controller.UpsertReview(Guid.NewGuid(), new ReviewApiController.ReviewRequest(3.7m, "x"), default);

        _ = await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
        reviews.Verify(r => r.Upsert(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Delete_RemovesOwnReview()
    {
        var reviews = new Mock<IVariantReviewInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        _ = reviews.Setup(r => r.DeleteOwn(variantGuid, memberGuid)).Returns(true);
        var controller = new ReviewApiController(reviews.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.DeleteReview(variantGuid);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        reviews.Verify(r => r.DeleteOwn(variantGuid, memberGuid), Times.Once);
    }
}
