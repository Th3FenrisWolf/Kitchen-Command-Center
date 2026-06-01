using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
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
    private Dictionary<string, string> RecipeSearchStrings => resourceStrings.GetManyOrDefault(
        "RecipeSearch.SearchRecipes",
        "RecipeSearch.CreateRecipe"
    );

    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

        var page = (await contentRetriever.RetrievePages<RecipeListingPage>(
            new(),
            query => query
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId))
                .TopN(1),
            new($"{nameof(RecipeSearchController)}|{nameof(Index)}|{pageId}")
        )).FirstOrDefault();

        var recipes = await contentRetriever.RetrievePages<Recipe>(
            new() { LinkedItemsMaxLevel = 1 },
            null,
            new($"{nameof(RecipeSearchController)}|Recipes")
        );

        var createRecipePage = (await contentRetriever.RetrievePages<CreateRecipePage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(RecipeSearchController)}|CreateRecipePage")
        )).FirstOrDefault();

        var categoryGuids = recipes
            .SelectMany(r => r.Categories?.Select(c => c.Identifier) ?? [])
            .Distinct();

        var categoryResult = await taxonomyRetriever.RetrieveTags(categoryGuids, preferredLanguageRetriever.Get());
        var resolvedCategories = categoryResult?.ToDictionary(t => t.Identifier, t => t.Title) ?? [];

        var viewModel = new RecipeSearchViewModel()
        {
            CreateRecipeUrl = createRecipePage?.GetUrl().RelativePath,
            ResourceStrings = RecipeSearchStrings,
        };

        page?.MapMetadata(viewModel);
        page?.MapWebPageFields(viewModel);

        foreach (var recipe in recipes)
        {
            var variants = await contentRetriever.RetrievePages<RecipeVariant>(
                new(),
                query => query.Where(w =>
                    w.WhereEquals(
                        nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID),
                        recipe.SystemFields.WebPageItemID)),
                new($"{nameof(RecipeSearchController)}|Variants|{recipe.SystemFields.WebPageItemID}")
            );

            var categoryRef = recipe.Categories?.FirstOrDefault();

            viewModel.Recipes.Add(new RecipeSummaryViewModel
            {
                Name = recipe.Name,
                Description = recipe.Description,
                Image = recipe.Image?.FirstOrDefault()?.Asset?.Url,
                Category = categoryRef is not null && resolvedCategories.TryGetValue(categoryRef.Identifier, out var catName) ? catName : null,
                Slug = recipe.GetUrl().RelativePath,
                VariantCount = variants.Count(),
            });
        }

        return View("~/Features/Pages/RecipeSearch/Index.cshtml", viewModel);
    }
}
