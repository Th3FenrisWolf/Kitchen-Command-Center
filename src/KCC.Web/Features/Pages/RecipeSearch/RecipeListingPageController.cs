using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeListingPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IRecipeSearchService recipeSearch,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        if (CurrentPage is not RecipeListingPage listing)
        {
            return NotFound();
        }

        var viewModel = new RecipeSearchViewModel
        {
            CreateRecipeUrl = recipes.GetCreateRecipeUrl(listing),
            InitialResults = RecipeSearchResponseMapper.ToResponse(recipeSearch.Search(new RecipeSearchCriteria())),
            Breadcrumbs = breadcrumbs.Build(listing),
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(listing, viewModel);

        return View("~/Features/Pages/RecipeSearch/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeSearch.SearchRecipes",
        "RecipeSearch.CreateRecipe",
        "RecipeSearch.BrowseTheKitchen",
        "RecipeSearch.SearchPlaceholder",
        "RecipeSearch.Search",
        "RecipeSearch.Filters",
        "RecipeSearch.Reset",
        "RecipeSearch.Category",
        "RecipeSearch.Dietary",
        "RecipeSearch.TotalTime",
        "RecipeSearch.Min",
        "RecipeSearch.OrMore",
        "RecipeSearch.OrLess",
        "RecipeSearch.Sort",
        "RecipeSearch.SortRelevant",
        "RecipeSearch.SortTopRated",
        "RecipeSearch.SortVariants",
        "RecipeSearch.SortRecent",
        "RecipeSearch.Grid",
        "RecipeSearch.List",
        "RecipeSearch.ClearAll",
        "RecipeSearch.TopRated",
        "RecipeSearch.Variants",
        "RecipeSearch.StartedBy",
        "RecipeSearch.NoRatingsYet",
        "RecipeSearch.LoadingMore",
        "RecipeSearch.NoRecipesMatch",
        "RecipeSearch.NoRecipesHint",
        "RecipeSearch.ClearAllFilters",
        "RecipeSearch.IngredientSearchComingSoon",
        "RecipeSearch.Recipe",
        "RecipeSearch.Recipes",
        "RecipeSearch.ResultsFor");
}
