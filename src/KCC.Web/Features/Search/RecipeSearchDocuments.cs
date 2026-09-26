using KCC.Contributions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Search;

public static class RecipeSearchDocuments
{
    public static IEnumerable<Guid> AuthorKeys(IEnumerable<RecipePageData> recipes) =>
        recipes.Select(page => page.Recipe.AuthorKey).OfType<Guid>().Distinct();

    public static RecipeSearchDocument From(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames)
    {
        var recipe = page.Recipe;
        var rating = stats.RatingAcross(page.Variants.Select(variant => variant.Key));

        return new RecipeSearchDocument
        {
            Name = recipe.Name ?? string.Empty,
            Slug = recipe.Url ?? string.Empty,
            Icon = recipe.Icon ?? string.Empty,
            Category = recipe.Category ?? string.Empty,
            StartedBy = AuthorNameProvider.NameFor(authorNames, recipe.AuthorKey) ?? string.Empty,
            Description = recipe.Description ?? string.Empty,
            Diets = page.Variants.SelectMany(variant => variant.Tags).Distinct().ToArray(),
            IngredientNames = page.Variants
                .SelectMany(variant => IngredientNames(variant.IngredientsJson))
                .Distinct()
                .ToArray(),
            FastestTime = RecipeSearchDocument.FastestOf(
                page.Variants.Select(variant => (variant.PrepTime, variant.CookTime)).ToArray()),
            VariantCount = page.Variants.Count,
            AverageRating = rating.Average,
            ReviewCount = rating.Count,
            PublishedUnixSeconds = new DateTimeOffset(PageMetadata.AsUtc(recipe.CreateDate, TimeZoneInfo.Local)).ToUnixTimeSeconds(),
        };
    }

    // The owner can type this JSON by hand in the backoffice; one slip must not take every recipe out of search.
    private static IEnumerable<string> IngredientNames(string json)
    {
        try
        {
            return JsonSerializer.DeserializeCollection<IngredientViewModel>(json)
                .Select(ingredient => ingredient.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
