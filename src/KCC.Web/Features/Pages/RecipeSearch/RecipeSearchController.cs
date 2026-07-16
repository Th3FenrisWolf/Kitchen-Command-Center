using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.RecipeSearch;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Search;
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
    IRecipeSearchService recipeSearch,
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

        var createRecipePage = await contentRetriever.RetrieveFirstPage<CreateRecipePage>();
        var initial = recipeSearch.Search(new RecipeSearchCriteria());

        var viewModel = new RecipeSearchViewModel
        {
            CreateRecipeUrl = createRecipePage?.GetUrl().RelativePath,
            InitialResults = RecipeSearchResponseMapper.ToResponse(initial),
            ResourceStrings = GetStrings(),
        };

        await page.MapMetadata(viewModel);
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
        "RecipeSearch.ResultsFor"
    );
}
