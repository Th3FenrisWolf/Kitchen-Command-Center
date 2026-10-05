using System.Globalization;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.Extensions.Primitives;

namespace KCC.Web.Features.Pages.RecipeSearch;

public static class RecipeLibraryFilters
{
    private static readonly string[] Sorts = ["relevant", "rated", "variants", "recent"];

    public static RecipeTaxonomy Options(RecipeTaxonomy taxonomy, RecipeFacetCounts counts) => new(
        taxonomy.Categories.Where(counts.Category.ContainsKey).ToList(),
        taxonomy.Diets.Where(counts.Diet.ContainsKey).ToList(),
        taxonomy.Styles.Where(counts.Style.ContainsKey).ToList());

    public static RecipeSearchCriteria FromQuery(IQueryCollection query, RecipeTaxonomy options) => new RecipeSearchCriteria
    {
        Query = query["query"].FirstOrDefault() ?? string.Empty,
        Categories = Offered(query["category"], options.Categories),
        Diets = Offered(query["diet"], options.Diets),
        Styles = Offered(query["style"], options.Styles),
        TimeMin = Number(query["timeMin"], 0),
        TimeMax = Number(query["timeMax"], RecipeSearchCriteria.MaxTime),
        Sort = Offered(query["sort"], Sorts).FirstOrDefault() ?? "relevant",
    }.Normalized();

    private static string[] Offered(StringValues values, IReadOnlyList<string> offered) =>
        values
            .Select(value => offered.FirstOrDefault(name => string.Equals(name, value?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(name => name is not null)
            .Distinct()
            .ToArray();

    private static int Number(StringValues values, int fallback) =>
        int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : fallback;
}
