using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeSearchViewModel : BasePageViewModel
{
    public List<RecipeSummaryViewModel> Recipes { get; set; } = [];
    public string CreateRecipeUrl { get; set; }
}

public class RecipeSummaryViewModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public string Category { get; set; }
    public string Slug { get; set; }
    public int VariantCount { get; set; }
}
