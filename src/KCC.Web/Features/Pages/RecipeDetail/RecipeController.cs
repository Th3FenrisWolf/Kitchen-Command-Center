using KCC.Contributions;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.RecipeDetail;

public class RecipeController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IAuthorNameProvider authorNames,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not Recipe recipe)
        {
            return NotFound();
        }

        var page = recipes.GetRecipePage(recipe);
        var viewModel = RecipeDetailMapping.Map(
            page,
            await contributionStats.GetAsync(),
            await authorNames.ResolveMany(RecipeDetailMapping.AuthorKeys(page)));
        viewModel.Breadcrumbs = breadcrumbs.Build(recipe);
        viewModel.ResourceStrings = GetStrings();
        pageMetadata.Apply(recipe, viewModel);

        return View("~/Features/Pages/RecipeDetail/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeDetail.AddVariant",
        "RecipeDetail.StartedBy",
        "RecipeDetail.Variants",
        "RecipeDetail.By",
        "RecipeDetail.ComingSoon",
        "RecipeDetail.AvgTime",
        "RecipeDetail.Fastest",
        "RecipeDetail.Contributors",
        "RecipeDetail.TopVariant",
        "RecipeDetail.RankingComingSoon",
        "RecipeDetail.AllVariants",
        "RecipeDetail.Sort",
        "RecipeDetail.SortNewest",
        "RecipeDetail.SortFastest",
        "RecipeDetail.SortTopRated",
        "RecipeDetail.Min",
        "RecipeDetail.SearchVariants",
        "RecipeDetail.Grid",
        "RecipeDetail.List",
        "RecipeDetail.Total",
        "RecipeDetail.Of",
        "RecipeDetail.NoVariantsMatch",
        "RecipeDetail.TryDifferentFilter",
        "RecipeDetail.ClearFilters",
        "RecipeDetail.TimesCooked",
        "RecipeDetail.NoRatingsYet",
        "RecipeDetail.Rating",
        "RecipeDetail.Reviews",
        "RecipeDetail.All");
}
