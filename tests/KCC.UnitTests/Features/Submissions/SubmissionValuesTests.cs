using System.Text.Json.Nodes;
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

    [Test]
    public async Task Recipe_WritesTheCategoryAsATreePickerReference()
    {
        var category = Guid.NewGuid();
        var request = Request("Eggs.");
        request.Category = category;

        var values = SubmissionValues.Recipe(request, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(((JsonArray)values["category"]).ToJsonString()).IsEqualTo($$"""[{"type":"document","unique":"{{category}}"}]""");
    }

    [Test]
    public async Task Recipe_LeavesOutAnEmptyCategoryAndBlankProposal()
    {
        var request = Request("Eggs.");
        request.ProposedCategory = "   ";

        var aliases = SubmissionValues.Recipe(request, "fa-duotone fa-egg", Author).Select(value => value.Alias).ToList();

        _ = await Assert.That(aliases).DoesNotContain("category");
        _ = await Assert.That(aliases).DoesNotContain("proposedCategory");
    }

    [Test]
    public async Task Recipe_WritesTheTrimmedProposedCategory()
    {
        var request = Request("Eggs.");
        request.ProposedCategory = " Sides ";

        var values = SubmissionValues.Recipe(request, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(values["proposedCategory"]).IsEqualTo("Sides");
    }

    [Test]
    public async Task Variant_WritesTagsInListOrderIgnoringDuplicates()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var variant = Variant();
        variant.Tags = [first, second, first];

        var values = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(((JsonArray)values["tags"]).ToJsonString())
            .IsEqualTo($$"""[{"type":"document","unique":"{{first}}"},{"type":"document","unique":"{{second}}"}]""");
    }

    [Test]
    public async Task Variant_WritesDifficultyAsTheDropdownsStoredArray()
    {
        var variant = Variant();
        variant.Difficulty = "Medium";

        var values = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(values["difficulty"]).IsEquivalentTo(new[] { "Medium" });
    }

    [Test]
    public async Task Variant_WritesEachNutritionValueGivenIncludingZeroAndLeavesOutTheRest()
    {
        var variant = Variant();
        variant.Nutrition = new NutritionDto { Calories = 450, ProteinG = 12, FatG = 0, SodiumMg = 380 };

        var values = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(values["calories"]).IsEqualTo(450);
        _ = await Assert.That(values["proteinG"]).IsEqualTo(12);
        _ = await Assert.That(values["fatG"]).IsEqualTo(0);
        _ = await Assert.That(values["sodiumMg"]).IsEqualTo(380);
        _ = await Assert.That(values.ContainsKey("carbsG")).IsFalse();
        _ = await Assert.That(values.ContainsKey("saturatedFatG")).IsFalse();
        _ = await Assert.That(values.ContainsKey("fiberG")).IsFalse();
        _ = await Assert.That(values.ContainsKey("sugarG")).IsFalse();
    }

    [Test]
    public async Task Variant_WritesTheTrimmedProposedTagsAndLeavesOutABlankOne()
    {
        var variant = Variant();
        variant.ProposedTags = " Mexican, Tex-Mex ";
        var written = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);
        _ = await Assert.That(written["proposedTags"]).IsEqualTo("Mexican, Tex-Mex");

        variant.ProposedTags = "  ";
        var aliases = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).Select(value => value.Alias).ToList();
        _ = await Assert.That(aliases).DoesNotContain("proposedTags");
    }

    [Test]
    public async Task Variant_WithNoDetailsWritesExactlyThePhaseOneValues()
    {
        var aliases = SubmissionValues.Variant(Variant(), "fa-duotone fa-egg", Author).Select(value => value.Alias).Order().ToList();

        _ = await Assert.That(aliases).IsEquivalentTo(new[] { "author", "icon", "ingredients", "instructions", "description" }.Order());
    }

    [Test]
    public async Task Recipe_WithNoDetailsWritesExactlyThePhaseOneValues()
    {
        var aliases = SubmissionValues.Recipe(Request("Eggs."), "fa-duotone fa-egg", Author).Select(value => value.Alias).ToList();

        _ = await Assert.That(aliases).IsEquivalentTo(new[] { "author", "icon", "description" });
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
