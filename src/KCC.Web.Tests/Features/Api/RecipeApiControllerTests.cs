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
using Xunit;

namespace KCC.Web.Tests.Features.Api;

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

    [Fact]
    public void BuildRecipeData_StampsAuthorMemberGuid()
    {
        var author = Guid.NewGuid();

        var data = RecipeApiController.BuildRecipeData(RecipeRequest(), "fa-icon", author);

        Assert.Equal(author, data[nameof(KCC.Recipe.AuthorMemberGuid)]);
        Assert.Equal("Mac & Cheese", data[nameof(KCC.Recipe.Name)]);
        Assert.Equal("fa-icon", data[nameof(KCC.Recipe.Icon)]);
        Assert.Equal("Cheesy goodness", data[nameof(KCC.Recipe.Description)]);
    }

    [Fact]
    public void BuildVariantData_StampsAuthorMemberGuid()
    {
        var author = Guid.NewGuid();

        var data = RecipeApiController.BuildVariantData(VariantRequest(), "fa-icon", author);

        Assert.Equal(author, data[nameof(KCC.RecipeVariant.AuthorMemberGuid)]);
        Assert.Equal("Classic", data[nameof(KCC.RecipeVariant.Name)]);
        Assert.Equal(10, data[nameof(KCC.RecipeVariant.PrepTime)]);
        Assert.Equal("The classic take", data[nameof(KCC.RecipeVariant.Description)]);
        Assert.Equal("fa-icon", data[nameof(KCC.RecipeVariant.Icon)]);
        Assert.Equal(20, data[nameof(KCC.RecipeVariant.CookTime)]);
        Assert.Equal(4, data[nameof(KCC.RecipeVariant.ServingNumber)]);
        Assert.Contains("elbow macaroni", (string)data[nameof(KCC.RecipeVariant.Ingredients)]);
        Assert.Contains("Boil it.", (string)data[nameof(KCC.RecipeVariant.Instructions)]);
    }

    [Fact]
    public async Task CreateRecipe_ReturnsUnauthorizedWhenNoCurrentMember()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((KCCApplicationUser)null);
        var controller = Build(userManager);

        var result = await controller.CreateRecipe(RecipeRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task AddVariant_ReturnsUnauthorizedWhenNoCurrentMember()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((KCCApplicationUser)null);
        var controller = Build(userManager);

        var result = await controller.AddVariant(1, VariantRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
