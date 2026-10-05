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

    public static RecipeSearchDocument From(
        RecipePageData page,
        ContributionStats stats,
        IReadOnlyDictionary<Guid, string> authorNames,
        IReadOnlySet<string> styleTags)
    {
        var recipe = page.Recipe;
        var rating = stats.RatingAcross(page.Variants.Select(variant => variant.Key));
        var tags = page.Variants.SelectMany(variant => variant.Tags).Distinct().ToArray();
        var styles = tags.Where(styleTags.Contains).ToArray();

        return new RecipeSearchDocument
        {
            Name = recipe.Name ?? string.Empty,
            Slug = recipe.Url ?? string.Empty,
            Icon = recipe.Icon ?? string.Empty,
            Category = recipe.Category ?? string.Empty,
            StartedBy = AuthorNameProvider.NameFor(authorNames, recipe.AuthorKey) ?? string.Empty,
            Description = recipe.Description ?? string.Empty,
            Tags = tags,
            Diets = tags.Except(styles).ToArray(),
            Styles = styles,
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

    private static IEnumerable<string> IngredientNames(string json) =>
        JsonSerializer.DeserializeCollection<IngredientViewModel>(json)
            .Select(ingredient => ingredient.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
}
