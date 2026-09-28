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
    IAccountPageQueries accountPages,
    IRecipeQueries recipes,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

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
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/AddVariant/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        // Hero + shared navigation
        "AddVariant.AddVariantFor",
        "AddVariant.Cancel",
        "AddVariant.Next",
        "AddVariant.Back",
        // Step 1: variant info
        "AddVariant.VariantInfo",
        "AddVariant.VariantName",
        "AddVariant.Description",
        "AddVariant.DescriptionPlaceholder",
        "AddVariant.PrepTime",
        "AddVariant.CookTime",
        "AddVariant.Servings",
        // Step 2: ingredients
        "AddVariant.Ingredients",
        "AddVariant.IngredientName",
        "AddVariant.IngredientNamePlaceholder",
        "AddVariant.Eyeball",
        "AddVariant.Quantity",
        "AddVariant.QuantityPlaceholder",
        "AddVariant.Unit",
        "AddVariant.UnitPlaceholder",
        "AddVariant.Remove",
        "AddVariant.AddIngredient",
        // Step 3: instructions
        "AddVariant.Instructions",
        "AddVariant.DescribeThisStep",
        "AddVariant.AddStep",
        // Step 4: review & submit
        "AddVariant.ReviewAndSubmit",
        "AddVariant.Min",
        "AddVariant.Serves",
        "AddVariant.ToTaste",
        "AddVariant.Step",
        "AddVariant.Steps",
        "AddVariant.Submitting",
        "AddVariant.SubmitForReview",
        // Success + error states
        "AddVariant.VariantSubmitted",
        "AddVariant.VariantSubmittedMessage",
        "AddVariant.BackTo",
        "AddVariant.FailedToAddVariant",
        "AddVariant.UnexpectedError");
}
