using KCC.Web.Features.Pages.RecipeVariantDetail;

namespace KCC.UnitTests.Features.Pages.RecipeVariantDetail;

public class RecipeVariantViewModelTests
{
    [Test]
    public async Task DeserializeIngredients_ParsesJsonCorrectly()
    {
        var json = """
            [
                {"name":"elbow macaroni","quantity":2,"unit":"cups","isEyeballed":false},
                {"name":"butter","quantity":3,"unit":"tbsp","isEyeballed":true}
            ]
            """;

        var ingredients = RecipeVariantViewModel.DeserializeIngredients(json);

        _ = await Assert.That(ingredients.Count).IsEqualTo(2);
        _ = await Assert.That(ingredients[0].Name).IsEqualTo("elbow macaroni");
        _ = await Assert.That(ingredients[0].Quantity).IsEqualTo(2);
        _ = await Assert.That(ingredients[0].IsEyeballed).IsFalse();
        _ = await Assert.That(ingredients[1].IsEyeballed).IsTrue();
    }

    [Test]
    public async Task DeserializeIngredients_ReturnsEmptyForNullOrEmpty()
    {
        _ = await Assert.That(RecipeVariantViewModel.DeserializeIngredients(null).Count()).IsEqualTo(0);
        _ = await Assert.That(RecipeVariantViewModel.DeserializeIngredients("").Count()).IsEqualTo(0);
    }

    [Test]
    public async Task DeserializeInstructions_ParsesJsonCorrectly()
    {
        var json = """
            [
                {"step":1,"text":"Boil water."},
                {"step":2,"text":"Add pasta."}
            ]
            """;

        var instructions = RecipeVariantViewModel.DeserializeInstructions(json);

        _ = await Assert.That(instructions.Count).IsEqualTo(2);
        _ = await Assert.That(instructions[0].Step).IsEqualTo(1);
        _ = await Assert.That(instructions[0].Text).IsEqualTo("Boil water.");
    }

    [Test]
    public async Task DeserializeInstructions_ReturnsEmptyForNullOrEmpty()
    {
        _ = await Assert.That(RecipeVariantViewModel.DeserializeInstructions(null).Count()).IsEqualTo(0);
        _ = await Assert.That(RecipeVariantViewModel.DeserializeInstructions("").Count()).IsEqualTo(0);
    }
}
