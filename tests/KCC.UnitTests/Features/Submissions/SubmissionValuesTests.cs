using KCC.Web.Features.Api;
using KCC.Web.Features.Submissions;

namespace KCC.UnitTests.Features.Submissions;

public class SubmissionValuesTests
{
    private static readonly Guid Author = Guid.NewGuid();

    [Test]
    public async Task Recipe_CarriesItsIconAuthorAndTrimmedDescription()
    {
        var values = SubmissionValues.Recipe(Request(" A weeknight staple. "), "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(values["icon"]).IsEqualTo("fa-duotone fa-egg");
        _ = await Assert.That(values["author"]).IsEqualTo(Author.ToString());
        _ = await Assert.That(values["description"]).IsEqualTo("A weeknight staple.");
    }

    [Test]
    public async Task Recipe_LeavesOutABlankDescription()
    {
        var values = SubmissionValues.Recipe(Request("   "), "fa-duotone fa-egg", Author);

        _ = await Assert.That(values.Any(value => value.Alias == "description")).IsFalse();
    }

    [Test]
    public async Task Variant_LeavesOutTimesTheMemberDidNotGive()
    {
        var variant = Variant();
        variant.PrepTime = 10;
        variant.CookTime = null;
        variant.Servings = 0;

        var aliases = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).Select(value => value.Alias).ToList();

        _ = await Assert.That(aliases).Contains("prepTime");
        _ = await Assert.That(aliases).DoesNotContain("cookTime");
        _ = await Assert.That(aliases).DoesNotContain("servings");
    }

    [Test]
    public async Task IngredientsJson_GivesAnEyeballedRowNoQuantityOrUnit()
    {
        var json = SubmissionValues.IngredientsJson(
        [
            new IngredientDto { Name = " Flour ", Quantity = 2, Unit = " Cups ", IsEyeballed = false },
            new IngredientDto { Name = "Salt", Quantity = 1, Unit = "Pinch", IsEyeballed = true },
            new IngredientDto { Name = " ", Quantity = 3, Unit = "Cups", IsEyeballed = false },
        ]);

        _ = await Assert.That(json).IsEqualTo("""[{"name":"Flour","quantity":2,"unit":"Cups","isEyeballed":false},{"name":"Salt","quantity":null,"unit":"","isEyeballed":true}]""");
    }

    [Test]
    public async Task InstructionsJson_NumbersTheStepsThatHaveText()
    {
        var json = SubmissionValues.InstructionsJson(
        [
            new InstructionDto { Step = 4, Text = " Whisk. " },
            new InstructionDto { Step = 5, Text = "  " },
            new InstructionDto { Step = 9, Text = "Bake." },
        ]);

        _ = await Assert.That(json).IsEqualTo("""[{"step":1,"text":"Whisk."},{"step":2,"text":"Bake."}]""");
    }

    private static CreateRecipeRequest Request(string description) => new()
    {
        RecipeName = "Shakshuka",
        RecipeDescription = description,
        FirstVariant = Variant(),
    };

    private static CreateVariantRequest Variant() => new()
    {
        VariantName = "Classic",
        VariantDescription = "Eggs in tomato.",
        Ingredients = [new IngredientDto { Name = "Eggs", Quantity = 4, Unit = "Whole" }],
        Instructions = [new InstructionDto { Step = 1, Text = "Simmer." }],
    };
}
