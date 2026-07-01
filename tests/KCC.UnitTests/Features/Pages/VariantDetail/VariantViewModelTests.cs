using KCC.Web.Features.Helpers;
using KCC.Web.Features.Pages.VariantDetail;

namespace KCC.UnitTests.Features.Pages.VariantDetail;

public class VariantViewModelTests
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

        var ingredients = JsonSerializer.DeserializeCollection<IngredientViewModel>(json);

        _ = await Assert.That(ingredients.Count()).IsEqualTo(2);
        _ = await Assert.That(ingredients.ElementAt(0).Name).IsEqualTo("elbow macaroni");
        _ = await Assert.That(ingredients.ElementAt(0).Quantity).IsEqualTo(2);
        _ = await Assert.That(ingredients.ElementAt(0).IsEyeballed).IsFalse();
        _ = await Assert.That(ingredients.ElementAt(1).IsEyeballed).IsTrue();
    }

    [Test]
    public async Task DeserializeIngredients_ReturnsEmptyForNullOrEmpty()
    {
        _ = await Assert.That(JsonSerializer.DeserializeCollection<IngredientViewModel>(null).Count()).IsEqualTo(0);
        _ = await Assert.That(JsonSerializer.DeserializeCollection<IngredientViewModel>("").Count()).IsEqualTo(0);
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

        var instructions = JsonSerializer.DeserializeCollection<InstructionViewModel>(json);

        _ = await Assert.That(instructions.Count).IsEqualTo(2);
        _ = await Assert.That(instructions.ElementAt(0).Step).IsEqualTo(1);
        _ = await Assert.That(instructions.ElementAt(0).Text).IsEqualTo("Boil water.");
    }

    [Test]
    public async Task DeserializeInstructions_ReturnsEmptyForNullOrEmpty()
    {
        _ = await Assert.That(JsonSerializer.DeserializeCollection<InstructionViewModel>(null).Count()).IsEqualTo(0);
        _ = await Assert.That(JsonSerializer.DeserializeCollection<InstructionViewModel>("").Count()).IsEqualTo(0);
    }
}
