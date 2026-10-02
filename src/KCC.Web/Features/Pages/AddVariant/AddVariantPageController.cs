using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.AddVariant;

public class AddVariantPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    AccountPageQueries accountPages,
    IRecipeQueries recipes,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public async Task<IActionResult> Index([FromQuery(Name = "recipe")] Guid? recipeKey, CancellationToken cancellationToken)
    {
        if (CurrentPage is not AddVariantPage page)
        {
            return NotFound();
        }

        if (await memberManager.GetCurrentMemberAsync() is null)
        {
            return SignInRedirect.To(accountPages.GetUrls().Login, Request);
        }

        if (recipeKey is not { } key || recipes.FindPublishedRecipe(key) is not { } recipe)
        {
            return NotFound();
        }

        var viewModel = new AddVariantViewModel
        {
            RecipeId = recipe.Key,
            RecipeName = recipe.Name,
            RecipeSlug = recipe.Url,
            ResourceStrings = resourceStrings.GetGroup("AddVariant"),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/AddVariant/Index.cshtml", viewModel);
    }
}
