using System.Text.Json;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.RecipeImport;

public class DuplicateVariantCheckTests
{
    private static readonly string[] FiveIngredients = ["flour", "sugar", "eggs", "butter", "milk"];

    [Test]
    public async Task Names_AreComparedInLowerCase_WithADashOrSlashAsASpace_OtherPunctuationRemoved_WhitespaceCollapsed_AndATrailingSDropped()
    {
        var stored = Variant(
            "Classic",
            ["egg", "brown sugar", "chefs salt", "pea", "self raising flour", "all purpose flour", "oil vinegar dressing"],
            steps: 3);
        var pancakes = Page("Pancakes", stored);

        var hit = DuplicateVariantCheck.Find(
            ["Eggs", "  Brown   Sugar.", "Chef's Salt", "PEAS", "Self-raising flours", "All-purpose flour", "Oil/vinegar dressing"],
            3,
            [pancakes]);

        _ = await Assert.That(hit).IsEqualTo(new PossibleDuplicate(stored.Key, "Classic", "Pancakes"));
    }

    [Test]
    public async Task ATrailingS_StaysOnAWordOfThreeLettersOrFewer()
    {
        var stored = Variant("Classic", ["ga", "bu", "pea", "flour", "salt"], steps: 2);

        var hit = DuplicateVariantCheck.Find(["gas", "bus", "peas", "flour", "salt"], 2, [Page("Stew", stored)]);

        _ = await Assert.That(hit).IsNull();
    }

    [Test]
    public async Task AnOverlapOf80Percent_IsAPossibleDuplicate()
    {
        var stored = Variant("Classic", FiveIngredients, steps: 4);

        var hit = DuplicateVariantCheck.Find(["flour", "sugar", "eggs", "butter"], 4, [Page("Cake", stored)]);

        _ = await Assert.That(hit?.VariantKey).IsEqualTo(stored.Key);
    }

    [Test]
    public async Task AnOverlapUnder80Percent_IsNone()
    {
        var stored = Variant("Classic", ["flour", "sugar", "eggs", "butter"], steps: 4);

        var hit = DuplicateVariantCheck.Find(["flour", "sugar", "eggs"], 4, [Page("Cake", stored)]);

        _ = await Assert.That(hit).IsNull();
    }

    [Test]
    [Arguments(2, true)]
    [Arguments(6, true)]
    [Arguments(1, false)]
    [Arguments(7, false)]
    public async Task TheStepCounts_MayDifferByAtMostTwo(int readSteps, bool isDuplicate)
    {
        var stored = Variant("Classic", FiveIngredients, steps: 4);

        var hit = DuplicateVariantCheck.Find(FiveIngredients, readSteps, [Page("Cake", stored)]);

        _ = await Assert.That(hit is not null).IsEqualTo(isDuplicate);
    }

    [Test]
    public async Task TheHighestOverlap_WinsAcrossTheVariantsOfEveryRecipe()
    {
        var close = Variant("Close", [.. FiveIngredients, "vanilla"], steps: 4);
        var exact = Variant("Exact", FiveIngredients, steps: 4);
        var far = Variant("Far", ["rice", "beans"], steps: 4);

        var hit = DuplicateVariantCheck.Find(FiveIngredients, 4, [Page("Cake", close), Page("Sponge", far, exact)]);

        _ = await Assert.That(hit).IsEqualTo(new PossibleDuplicate(exact.Key, "Exact", "Sponge"));
    }

    [Test]
    public async Task AnEqualOverlap_GoesToTheRecipeThatComesFirst_ThenToTheVariantThatComesFirstInIt()
    {
        var firstOfFirst = Variant("First of first", FiveIngredients, steps: 4);
        var secondOfFirst = Variant("Second of first", FiveIngredients, steps: 4);
        var firstOfSecond = Variant("First of second", FiveIngredients, steps: 4);

        var hit = DuplicateVariantCheck.Find(
            FiveIngredients,
            4,
            [Page("Zucchini Cake", firstOfFirst, secondOfFirst), Page("Apple Cake", firstOfSecond)]);
        var inTheOtherOrder = DuplicateVariantCheck.Find(
            FiveIngredients,
            4,
            [Page("Zucchini Cake", secondOfFirst, firstOfFirst), Page("Apple Cake", firstOfSecond)]);

        _ = await Assert.That(hit).IsEqualTo(new PossibleDuplicate(firstOfFirst.Key, "First of first", "Zucchini Cake"));
        _ = await Assert.That(inTheOtherOrder?.VariantKey).IsEqualTo(secondOfFirst.Key);
    }

    [Test]
    public async Task AReadWithNoIngredientName_IsNoDuplicate()
    {
        var stored = Variant("Empty", [], steps: 0);

        var hit = DuplicateVariantCheck.Find(["", "  ", "!"], 0, [Page("Nothing", stored)]);

        _ = await Assert.That(hit).IsNull();
    }

    [Test]
    public async Task NoRecipes_GiveNoDuplicate()
    {
        _ = await Assert.That(DuplicateVariantCheck.Find(FiveIngredients, 4, [])).IsNull();
    }

    private static RecipePageData Page(string name, params VariantRecord[] variants) => new(
        new RecipeRecord(Guid.NewGuid(), name, null, null, null, null, null, null, default),
        variants,
        null);

    private static VariantRecord Variant(string name, string[] ingredients, int steps) => new(
        Guid.NewGuid(),
        name,
        null,
        null,
        null,
        null,
        0,
        0,
        1,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        [],
        JsonSerializer.Serialize(ingredients.Select(ingredient => new { name = ingredient, quantity = 1, unit = "cup", isEyeballed = false })),
        JsonSerializer.Serialize(Enumerable.Range(1, steps).Select(step => new { step, text = $"Step {step}." })),
        null,
        default);
}
