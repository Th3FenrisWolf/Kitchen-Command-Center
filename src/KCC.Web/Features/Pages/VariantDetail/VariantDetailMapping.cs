using KCC.Contributions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Pages.VariantDetail;

public static class VariantDetailMapping
{
    public static IEnumerable<Guid> AuthorKeys(VariantPageData page) =>
        new[] { page.Variant.AuthorKey }.OfType<Guid>();

    public static VariantDetailViewModel Map(
        VariantPageData page,
        ContributionStats stats,
        IReadOnlyDictionary<Guid, string> authorNames,
        bool hasCooked,
        bool isAuthenticated)
    {
        var variant = page.Variant;
        var variantStats = stats.For(variant.Key);

        return new VariantDetailViewModel
        {
            VariantName = variant.Name,
            VariantDescription = variant.Description,
            Icon = variant.Icon,
            CoverImage = variant.ImageUrl,
            PrepTime = variant.PrepTime,
            CookTime = variant.CookTime,
            Servings = variant.Servings,
            Difficulty = variant.Difficulty,
            Calories = variant.Nutrition.Calories,
            ProteinG = variant.Nutrition.ProteinG,
            CarbsG = variant.Nutrition.CarbsG,
            FatG = variant.Nutrition.FatG,
            SaturatedFatG = variant.Nutrition.SaturatedFatG,
            FiberG = variant.Nutrition.FiberG,
            SugarG = variant.Nutrition.SugarG,
            SodiumMg = variant.Nutrition.SodiumMg,
            Tags = variant.Tags,
            Ingredients = JsonSerializer.DeserializeCollection<IngredientViewModel>(variant.IngredientsJson),
            Instructions = JsonSerializer.DeserializeCollection<InstructionViewModel>(variant.InstructionsJson),
            VariantSlug = variant.Url,
            RecipeName = page.Recipe.Name,
            RecipeSlug = page.Recipe.Url,
            CreatedByName = AuthorNameProvider.NameFor(authorNames, variant.AuthorKey),
            VariantGuid = variant.Key,
            AverageRating = variantStats.Rating.Average,
            ReviewCount = variantStats.Rating.Count,
            CookedCount = variantStats.CookedCount,
            HasCooked = hasCooked,
            IsAuthenticated = isAuthenticated,
            SiblingVariants = page.Siblings.Select(sibling => new SiblingVariantViewModel
            {
                Name = sibling.Name,
                Slug = sibling.Url,
                Icon = sibling.Icon,
                Rating = stats.For(sibling.Key).Rating.Average,
                TotalTime = sibling.TotalTime,
            }).ToList(),
        };
    }
}
