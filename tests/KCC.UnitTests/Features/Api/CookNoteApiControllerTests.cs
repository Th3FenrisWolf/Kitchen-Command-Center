using System.Security.Claims;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KCC.UnitTests.Features.Api;

public class CookNoteApiControllerTests
{
    private static UserManager<KCCApplicationUser> MockUserManager(KCCApplicationUser user)
    {
        var store = new Mock<IUserStore<KCCApplicationUser>>();
        var mgr = new Mock<UserManager<KCCApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _ = mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        return mgr.Object;
    }

    [Test]
    public async Task Post_RejectsEmptyText()
    {
        var notes = new Mock<IVariantCookNoteInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var controller = new CookNoteApiController(notes.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = Guid.NewGuid() }));

        var result = await controller.AddNote(Guid.NewGuid(), new CookNoteApiController.NoteRequest("  "), default);

        _ = await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
        notes.Verify(n => n.Add(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Delete_ReturnsForbiddenWhenNotAuthor()
    {
        var notes = new Mock<IVariantCookNoteInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var memberGuid = Guid.NewGuid();
        _ = notes.Setup(n => n.DeleteOwn(42, memberGuid)).Returns(false);
        var controller = new CookNoteApiController(notes.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.DeleteNote(42);

        _ = await Assert.That(result).IsTypeOf<ObjectResult>();
        var obj = (ObjectResult)result;
        _ = await Assert.That(obj.StatusCode).IsEqualTo(403);
    }

    [Test]
    public async Task Post_AddsWithResolvedRecipeGuid()
    {
        var notes = new Mock<IVariantCookNoteInfoProvider>();
        var resolver = new Mock<IVariantGuidResolver>();
        var variantGuid = Guid.NewGuid();
        var recipeGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        _ = resolver.Setup(r => r.ResolveRecipeGuidAsync(variantGuid, It.IsAny<CancellationToken>())).ReturnsAsync(recipeGuid);
        _ = notes.Setup(n => n.Add(variantGuid, recipeGuid, memberGuid, "use less salt")).Returns(7);
        var controller = new CookNoteApiController(notes.Object, resolver.Object, MockUserManager(new KCCApplicationUser { MemberGuid = memberGuid }));

        var result = await controller.AddNote(variantGuid, new CookNoteApiController.NoteRequest("use less salt"), default);

        _ = await Assert.That(result).IsTypeOf<OkObjectResult>();
        notes.Verify(n => n.Add(variantGuid, recipeGuid, memberGuid, "use less salt"), Times.Once);
    }
}
