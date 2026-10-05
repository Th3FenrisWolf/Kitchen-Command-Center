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

        var wholeLibrary = recipeSearch.Search(new RecipeSearchCriteria());
        var viewModel = new RecipeSearchViewModel
        {
            CreateRecipeUrl = recipes.GetCreateRecipeUrl(listing),
            InitialResults = wholeLibrary,
            Options = RecipeLibraryFilters.Options(recipes.GetTaxonomy(), wholeLibrary.Facets),
            Breadcrumbs = breadcrumbs.Build(listing),
            ResourceStrings = resourceStrings.GetGroup("RecipeSearch"),
        };
        pageMetadata.Apply(listing, viewModel);

        return View("~/Features/Pages/RecipeSearch/Index.cshtml", viewModel);
    }
}
