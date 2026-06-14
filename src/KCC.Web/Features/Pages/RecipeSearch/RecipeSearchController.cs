using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.RecipeSearch;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RecipeListingPage.CONTENT_TYPE_NAME,
    typeof(RecipeSearchController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeSearchController(
    IContentRetriever contentRetriever,
    IWebPageDataContextRetriever webPageDataContextRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IResourceStringInfoProvider resourceStrings
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;
        var page = await contentRetriever.RetrievePage<RecipeListingPage>(pageId);

        if (page is null)
        {
            return NotFound();
        }

        var createRecipePage = await contentRetriever.RetrievePage<CreateRecipePage>();

        var viewModel = new RecipeSearchViewModel()
        {
            CreateRecipeUrl = createRecipePage?.GetUrl().RelativePath,
            Recipes = await RetrieveRecipes(),
            ResourceStrings = GetStrings(),
        };

        await page.MapMetadata(viewModel);
        return View("~/Features/Pages/RecipeSearch/Index.cshtml", viewModel);
    }

    private async Task<IEnumerable<RecipeSummaryViewModel>> RetrieveRecipes()
    {
        var recipes = await contentRetriever.RetrievePages<Recipe>(
            new() { LinkedItemsMaxLevel = 1 },
            null,
            new($"{nameof(RecipeSearchController)}|{nameof(RetrieveRecipes)}")
        );

        var variants = await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query.Columns(nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID)),
            new($"{nameof(RecipeSearchController)}|{nameof(RetrieveRecipes)}|Variants"));

        var variantCounts = variants
            .GroupBy(v => v.SystemFields.WebPageItemParentID)
            .ToDictionary(g => g.Key, g => g.Count());

        var categoryGuids = recipes
            .SelectMany(r => r.Categories?.Select(c => c.Identifier) ?? [])
            .Distinct();

        var categoryResult = await taxonomyRetriever.RetrieveTags(categoryGuids, preferredLanguageRetriever.Get());
        var resolvedCategories = categoryResult?.ToDictionary(t => t.Identifier, t => t.Title) ?? [];

        return recipes.Select(recipe => new RecipeSummaryViewModel
        {
            Name = recipe.Name,
            Description = recipe.Description,
            Image = recipe.Image?.FirstOrDefault()?.Asset?.Url,
            Icon = recipe.Icon,
            Category = resolvedCategories.GetValueOrDefault(recipe.Categories?.FirstOrDefault()?.Identifier ?? Guid.Empty),
            Slug = recipe.GetUrl().RelativePath,
            VariantCount = variantCounts.GetValueOrDefault(recipe.SystemFields.WebPageItemID),
        });
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeSearch.SearchRecipes",
        "RecipeSearch.CreateRecipe"
    );
}
