using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchDocumentTests
{
    private static RecipeSearchDocument Sample() => new()
    {
        Name = "Weeknight Chicken Piccata",
        Slug = "/recipes/chicken-piccata",
        Icon = "fa-solid fa-drumstick-bite",
        Category = "Mains",
        StartedBy = "Dana Whitfield",
        Description = "Bright, buttery, caper-flecked.",
        Diets = ["Gluten-Free"],
        IngredientNames = ["chicken breast", "capers", "lemon"],
        FastestTime = 30,
        VariantCount = 5,
        AverageRating = 4.8,
        ReviewCount = 12,
        PublishedUnixSeconds = 1_700_000_000,
    };

    [Test]
    public async Task Content_CombinesDescriptionIngredientsAuthorAndCategory()
    {
        var content = RecipeSearchDocument.BuildContent(Sample());
        _ = await Assert.That(content).Contains("caper-flecked");
        _ = await Assert.That(content).Contains("capers");
        _ = await Assert.That(content).Contains("Dana Whitfield");
        _ = await Assert.That(content).Contains("Mains");
    }

    [Test]
    public async Task TagsStorage_JoinsWithSemicolons()
    {
        var doc = Sample() with { Diets = ["Vegan", "Dairy-Free"] };
        _ = await Assert.That(RecipeSearchDocument.JoinTags(doc.Diets)).IsEqualTo("Vegan;Dairy-Free");
    }

    [Test]
    public async Task FastestTime_IsMinOfVariantTotals()
    {
        _ = await Assert.That(RecipeSearchDocument.FastestOf([(10, 15), (5, 12), (20, 20)])).IsEqualTo(17);
    }

    [Test]
    public async Task FastestTime_ZeroWhenNoVariants()
    {
        _ = await Assert.That(RecipeSearchDocument.FastestOf([])).IsEqualTo(0);
    }
}
