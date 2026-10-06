using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeSearchViewModel : BasePageViewModel
{
    public RecipeSearchResults InitialResults { get; set; }

    public RecipeSearchCriteria Filters { get; set; }

    public RecipeTaxonomy Options { get; set; }

    public string CreateRecipeUrl { get; set; }
}
