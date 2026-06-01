using KCC.Web.Features.Pages.RecipeVariantDetail;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.RecipeVariantDetail;

public class RecipeVariantViewModelTests
{
    [Fact]
    public void DeserializeIngredients_ParsesJsonCorrectly()
    {
        var json = """
            [
                {"name":"elbow macaroni","quantity":2,"unit":"cups","isEyeballed":false},
                {"name":"butter","quantity":3,"unit":"tbsp","isEyeballed":true}
            ]
            """;

        var ingredients = RecipeVariantViewModel.DeserializeIngredients(json);

        Assert.Equal(2, ingredients.Count);
        Assert.Equal("elbow macaroni", ingredients[0].Name);
        Assert.Equal(2, ingredients[0].Quantity);
        Assert.False(ingredients[0].IsEyeballed);
        Assert.True(ingredients[1].IsEyeballed);
    }

    [Fact]
    public void DeserializeIngredients_ReturnsEmptyForNullOrEmpty()
    {
        Assert.Empty(RecipeVariantViewModel.DeserializeIngredients(null));
        Assert.Empty(RecipeVariantViewModel.DeserializeIngredients(""));
    }

    [Fact]
    public void DeserializeInstructions_ParsesJsonCorrectly()
    {
        var json = """
            [
                {"step":1,"text":"Boil water."},
                {"step":2,"text":"Add pasta."}
            ]
            """;

        var instructions = RecipeVariantViewModel.DeserializeInstructions(json);

        Assert.Equal(2, instructions.Count);
        Assert.Equal(1, instructions[0].Step);
        Assert.Equal("Boil water.", instructions[0].Text);
    }

    [Fact]
    public void DeserializeInstructions_ReturnsEmptyForNullOrEmpty()
    {
        Assert.Empty(RecipeVariantViewModel.DeserializeInstructions(null));
        Assert.Empty(RecipeVariantViewModel.DeserializeInstructions(""));
    }
}
