using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.AddVariant;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    AddVariantPage.CONTENT_TYPE_NAME,
    typeof(AddVariantController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.AddVariant;

[Authorize]
public class AddVariantController(
    IContentRetriever contentRetriever,
    IResourceStringInfoProvider resourceStrings
) : Controller
{
    private Dictionary<string, string> ResourceStrings => resourceStrings.GetManyOrDefault(
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
        "AddVariant.Steps",
        "AddVariant.Submitting",
        "AddVariant.SubmitForReview",
        // Success + error states
        "AddVariant.VariantSubmitted",
        "AddVariant.VariantSubmittedMessage",
        "AddVariant.BackTo",
        "AddVariant.FailedToAddVariant",
        "AddVariant.UnexpectedError"
    );

    public async Task<IActionResult> Index([FromQuery(Name = "recipe")] Guid? recipeGuid)
    {
        var isPreview = HttpContext.IsPreview() || HttpContext.IsPageBuilder();

        if (recipeGuid is null || recipeGuid == Guid.Empty)
        {
            return isPreview ? StubView() : NotFound();
        }

        var recipe = (await contentRetriever.RetrievePages<Recipe>(
            new(),
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), recipeGuid.Value))
                .TopN(1),
            new($"{nameof(AddVariantController)}|{nameof(Index)}|{recipeGuid}")
        )).FirstOrDefault();

        if (recipe is null)
        {
            return isPreview ? StubView() : NotFound();
        }

        var viewModel = new AddVariantViewModel
        {
            RecipeId = recipe.SystemFields.WebPageItemID,
            RecipeName = recipe.Name,
            RecipeSlug = recipe.GetUrl().RelativePath,
            ResourceStrings = ResourceStrings,
        };

        return View("~/Features/Pages/AddVariant/Index.cshtml", viewModel);
    }

    private ViewResult StubView() => View(
        "~/Features/Pages/AddVariant/Index.cshtml",
        new AddVariantViewModel
        {
            RecipeId = 0,
            RecipeName = "Sample Recipe",
            RecipeSlug = "/recipes/sample-recipe",
            ResourceStrings = ResourceStrings,
        });
}
