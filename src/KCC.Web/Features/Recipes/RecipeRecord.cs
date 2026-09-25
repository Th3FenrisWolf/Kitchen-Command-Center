namespace KCC.Web.Features.Recipes;

public sealed record RecipeRecord(
    Guid Key,
    string Name,
    string Url,
    string Description,
    string Icon,
    string ImageUrl,
    string Category,
    Guid? AuthorKey,
    DateTime CreateDate);

public sealed record NutritionRecord(
    int? Calories,
    int? ProteinG,
    int? CarbsG,
    int? FatG,
    int? SaturatedFatG,
    int? FiberG,
    int? SugarG,
    int? SodiumMg);

public sealed record VariantRecord(
    Guid Key,
    string Name,
    string Url,
    string Description,
    string Icon,
    string ImageUrl,
    int PrepTime,
    int CookTime,
    int Servings,
    string Difficulty,
    NutritionRecord Nutrition,
    IReadOnlyList<string> Tags,
    string IngredientsJson,
    string InstructionsJson,
    Guid? AuthorKey,
    DateTime CreateDate)
{
    public int TotalTime => PrepTime + CookTime;
}

public sealed record RecipePageData(RecipeRecord Recipe, IReadOnlyList<VariantRecord> Variants, string AddVariantUrl);

public sealed record VariantPageData(VariantRecord Variant, RecipeRecord Recipe, IReadOnlyList<VariantRecord> Siblings);
