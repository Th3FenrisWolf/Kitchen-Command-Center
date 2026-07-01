using System.Security.Claims;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Routing;
using KCC.Admin;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KCC.UnitTests.Features.Api;

public class RecipeApiControllerTests
{
    private static Mock<UserManager<KCCApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<KCCApplicationUser>>();
        return new Mock<UserManager<KCCApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static RecipeApiController Build(Mock<UserManager<KCCApplicationUser>> userManager) =>
        new(
            Mock.Of<IWebPageManagerFactory>(),
            Mock.Of<IWebsiteChannelContext>(),
            Mock.Of<IPreferredLanguageRetriever>(),
            Mock.Of<IUserInfoProvider>(),
            Mock.Of<IRecipeIconService>(),
            userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) },
            },
        };

    private static CreateVariantRequest VariantRequest() => new()
    {
        VariantName = "Classic",
        VariantDescription = "The classic take",
        PrepTime = 10,
        CookTime = 20,
        Servings = 4,
        Ingredients = [new() { Name = "elbow macaroni", Unit = "cups", Quantity = 2 }],
        Instructions = [new() { Step = 1, Text = "Boil it." }],
    };

    private static CreateRecipeRequest RecipeRequest() => new()
    {
        RecipeName = "Mac & Cheese",
        RecipeDescription = "Cheesy goodness",
        FirstVariant = VariantRequest(),
    };

    [Test]
    public async Task BuildRecipeData_StampsAuthorMemberGuid()
    {
        var author = Guid.NewGuid();

        var data = RecipeApiController.BuildRecipeData(RecipeRequest(), "fa-icon", author);

        _ = await Assert.That(data[nameof(Recipe.AuthorMemberGuid)]).IsEqualTo(author);
        _ = await Assert.That(data[nameof(Recipe.Name)]).IsEqualTo("Mac & Cheese");
        _ = await Assert.That(data[nameof(Recipe.Icon)]).IsEqualTo("fa-icon");
        _ = await Assert.That(data[nameof(Recipe.Description)]).IsEqualTo("Cheesy goodness");
    }

    [Test]
    public async Task BuildVariantData_StampsAuthorMemberGuid()
    {
        var author = Guid.NewGuid();

        var data = RecipeApiController.BuildVariantData(VariantRequest(), "fa-icon", author);

        _ = await Assert.That(data[nameof(RecipeVariant.AuthorMemberGuid)]).IsEqualTo(author);
        _ = await Assert.That(data[nameof(RecipeVariant.Name)]).IsEqualTo("Classic");
        _ = await Assert.That(data[nameof(RecipeVariant.PrepTime)]).IsEqualTo(10);
        _ = await Assert.That(data[nameof(RecipeVariant.Description)]).IsEqualTo("The classic take");
        _ = await Assert.That(data[nameof(RecipeVariant.Icon)]).IsEqualTo("fa-icon");
        _ = await Assert.That(data[nameof(RecipeVariant.CookTime)]).IsEqualTo(20);
        _ = await Assert.That(data[nameof(RecipeVariant.ServingNumber)]).IsEqualTo(4);
        _ = await Assert.That((string)data[nameof(RecipeVariant.Ingredients)]).Contains("elbow macaroni");
        _ = await Assert.That((string)data[nameof(RecipeVariant.Instructions)]).Contains("Boil it.");
    }

    [Test]
    public async Task CreateRecipe_ReturnsUnauthorizedWhenNoCurrentMember()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((KCCApplicationUser)null);
        var controller = Build(userManager);

        var result = await controller.CreateRecipe(RecipeRequest(), CancellationToken.None);

        _ = await Assert.That(result).IsTypeOf<UnauthorizedResult>();
    }

    [Test]
    public async Task AddVariant_ReturnsUnauthorizedWhenNoCurrentMember()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((KCCApplicationUser)null);
        var controller = Build(userManager);

        var result = await controller.AddVariant(1, VariantRequest(), CancellationToken.None);

        _ = await Assert.That(result).IsTypeOf<UnauthorizedResult>();
    }
}
