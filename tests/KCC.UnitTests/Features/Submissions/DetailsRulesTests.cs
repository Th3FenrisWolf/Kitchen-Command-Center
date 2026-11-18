using KCC.Web.Features.Api;
using KCC.Web.Features.Submissions;

namespace KCC.UnitTests.Features.Submissions;

public class DetailsRulesTests
{
    [Test]
    public async Task EmptyDetails_AreAccepted()
    {
        _ = await Assert.That(DetailsRules.RefusedField(Recipe(Variant()))).IsNull();
    }

    [Test]
    [Arguments("calories", 10_000, 10_001)]
    [Arguments("proteinG", 1_000, 1_001)]
    [Arguments("carbsG", 1_000, 1_001)]
    [Arguments("fatG", 1_000, 1_001)]
    [Arguments("saturatedFatG", 1_000, 1_001)]
    [Arguments("fiberG", 1_000, 1_001)]
    [Arguments("sugarG", 1_000, 1_001)]
    [Arguments("sodiumMg", 50_000, 50_001)]
    public async Task Nutrition_AcceptsZeroAndTheLimit_AndRefusesAboveItAndBelowZero(string field, int limit, int over)
    {
        _ = await Assert.That(DetailsRules.RefusedField(WithNutrition(field, 0))).IsNull();
        _ = await Assert.That(DetailsRules.RefusedField(WithNutrition(field, limit))).IsNull();
        _ = await Assert.That(DetailsRules.RefusedField(WithNutrition(field, over))).IsEqualTo(field);
        _ = await Assert.That(DetailsRules.RefusedField(WithNutrition(field, -1))).IsEqualTo(field);
    }

    [Test]
    public async Task Nutrition_AcceptsEmptyValues()
    {
        var variant = Variant();
        variant.Nutrition = new NutritionDto();

        _ = await Assert.That(DetailsRules.RefusedField(variant)).IsNull();
    }

    [Test]
    public async Task ProposedTags_AcceptsOneHundredCharactersAfterTrimming_AndRefusesMore()
    {
        var variant = Variant();
        variant.ProposedTags = " " + new string('a', 100) + " ";
        _ = await Assert.That(DetailsRules.RefusedField(variant)).IsNull();

        variant.ProposedTags = new string('a', 101);
        _ = await Assert.That(DetailsRules.RefusedField(variant)).IsEqualTo("proposedTags");
    }

    [Test]
    public async Task ProposedCategory_AcceptsOneHundredCharactersAfterTrimming_AndRefusesMore()
    {
        var recipe = Recipe(Variant());
        recipe.ProposedCategory = " " + new string('a', 100) + " ";
        _ = await Assert.That(DetailsRules.RefusedField(recipe)).IsNull();

        recipe.ProposedCategory = new string('a', 101);
        _ = await Assert.That(DetailsRules.RefusedField(recipe)).IsEqualTo("proposedCategory");
    }

    [Test]
    [Arguments("Easy", null)]
    [Arguments("Medium", null)]
    [Arguments("Hard", null)]
    [Arguments(null, null)]
    [Arguments("easy", "difficulty")]
    [Arguments("Medium ", "difficulty")]
    [Arguments("", "difficulty")]
    [Arguments("Expert", "difficulty")]
    public async Task Difficulty_MatchesExactly(string difficulty, string refused)
    {
        var variant = Variant();
        variant.Difficulty = difficulty;

        _ = await Assert.That(DetailsRules.RefusedField(variant)).IsEqualTo(refused);
    }

    [Test]
    public async Task RecipeCheck_ReachesTheFirstVariantsDetails()
    {
        var variant = Variant();
        variant.Nutrition = new NutritionDto { SodiumMg = 50_001 };

        _ = await Assert.That(DetailsRules.RefusedField(Recipe(variant))).IsEqualTo("sodiumMg");
    }

    private static CreateVariantRequest WithNutrition(string field, int value)
    {
        var nutrition = new NutritionDto();
        typeof(NutritionDto).GetProperty(char.ToUpperInvariant(field[0]) + field[1..])!.SetValue(nutrition, value);
        var variant = Variant();
        variant.Nutrition = nutrition;
        return variant;
    }

    private static CreateRecipeRequest Recipe(CreateVariantRequest variant) => new()
    {
        RecipeName = "Shakshuka",
        RecipeDescription = "Eggs in tomato.",
        FirstVariant = variant,
    };

    private static CreateVariantRequest Variant() => new()
    {
        VariantName = "Classic",
        VariantDescription = "Eggs in tomato.",
    };
}
