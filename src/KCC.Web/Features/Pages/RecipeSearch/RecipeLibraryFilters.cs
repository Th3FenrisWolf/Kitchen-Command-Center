using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;

namespace KCC.Web.Features.Pages.RecipeSearch;

public static class RecipeLibraryFilters
{
    public static RecipeTaxonomy Options(RecipeTaxonomy taxonomy, RecipeFacetCounts counts) => new(
        taxonomy.Categories.Where(counts.Category.ContainsKey).ToList(),
        taxonomy.Diets.Where(counts.Diet.ContainsKey).ToList(),
        taxonomy.Styles.Where(counts.Style.ContainsKey).ToList());
}
