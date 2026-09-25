using KCC.Contributions;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.Pages.VariantDetail;

public class VariantDetailMappingTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task Map_ReadsTheIngredientsAndInstructions()
    {
        var variant = Variant() with
        {
            IngredientsJson = """[{"name":"Flour","quantity":2,"unit":"cups","isEyeballed":false}]""",
            InstructionsJson = """[{"step":1,"text":"Whisk the batter."}]""",
        };

        var viewModel = Map(variant);

        _ = await Assert.That(viewModel.Ingredients.Single().Name).IsEqualTo("Flour");
        _ = await Assert.That(viewModel.Ingredients.Single().Quantity).IsEqualTo(2m);
        _ = await Assert.That(viewModel.Instructions.Single().Text).IsEqualTo("Whisk the batter.");
    }

    [Test]
    public async Task Map_MalformedIngredients_ShowsNone()
    {
        var viewModel = Map(Variant() with { IngredientsJson = "[{\"name\": \"Flour\"" });

        _ = await Assert.That(viewModel.Ingredients.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Map_KeepsUnsetNutritionUnset()
    {
        var viewModel = Map(Variant() with { Nutrition = new NutritionRecord(250, null, null, null, null, null, null, 0) });

        _ = await Assert.That(viewModel.Calories).IsEqualTo(250);
        _ = await Assert.That(viewModel.ProteinG).IsNull();
        _ = await Assert.That(viewModel.SodiumMg).IsEqualTo(0);
    }

    [Test]
    public async Task Map_RatesTheVariantAndEachSiblingByTheirOwnReviews()
    {
        var variant = Variant();
        var sibling = Variant("Blueberry Stack");
        var stats = ContributionStats.Build([(variant.Key, 5m), (sibling.Key, 3m), (sibling.Key, 4m)], [variant.Key]);
        var page = new VariantPageData(variant, Recipe(), [sibling]);

        var viewModel = VariantDetailMapping.Map(page, stats, NoNames, hasCooked: false, isAuthenticated: false);

        _ = await Assert.That(viewModel.AverageRating).IsEqualTo(5d);
        _ = await Assert.That(viewModel.ReviewCount).IsEqualTo(1);
        _ = await Assert.That(viewModel.CookedCount).IsEqualTo(1);
        _ = await Assert.That(viewModel.SiblingVariants.Single().Rating).IsEqualTo(3.5d);
        _ = await Assert.That(viewModel.SiblingVariants.Single().TotalTime).IsEqualTo(25);
    }

    [Test]
    public async Task Map_CarriesTheCoverImageTheRecipeAndTheSignedInState()
    {
        var variant = Variant() with { ImageUrl = "/media/stack.jpg?width=192" };
        var page = new VariantPageData(variant, Recipe(), []);

        var viewModel = VariantDetailMapping.Map(page, ContributionStats.Build([], []), NoNames, hasCooked: true, isAuthenticated: true);

        _ = await Assert.That(viewModel.CoverImage).IsEqualTo("/media/stack.jpg?width=192");
        _ = await Assert.That(viewModel.RecipeName).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(viewModel.RecipeSlug).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/");
        _ = await Assert.That(viewModel.VariantGuid).IsEqualTo(variant.Key);
        _ = await Assert.That(viewModel.HasCooked).IsTrue();
        _ = await Assert.That(viewModel.IsAuthenticated).IsTrue();
    }

    [Test]
    public async Task Map_NamesTheVariantAuthor()
    {
        var diego = Guid.NewGuid();
        var page = new VariantPageData(Variant() with { AuthorKey = diego }, Recipe(), []);

        var viewModel = VariantDetailMapping.Map(page, ContributionStats.Build([], []), new Dictionary<Guid, string> { [diego] = "Diego Salazar" }, false, false);

        _ = await Assert.That(viewModel.CreatedByName).IsEqualTo("Diego Salazar");
        _ = await Assert.That(string.Join(",", VariantDetailMapping.AuthorKeys(page))).IsEqualTo(diego.ToString());
    }

    private static VariantDetailViewModel Map(VariantRecord variant) =>
        VariantDetailMapping.Map(new VariantPageData(variant, Recipe(), []), ContributionStats.Build([], []), NoNames, false, false);

    private static RecipeRecord Recipe() => new(
        Guid.NewGuid(),
        "Fluffy Buttermilk Pancakes",
        "/recipes/fluffy-buttermilk-pancakes/",
        "Tall, tender stacks.",
        "fa-duotone fa-pancakes",
        null,
        "Breakfast",
        null,
        new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    private static VariantRecord Variant(string name = "Classic Stack") => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        null,
        10,
        15,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        ["Vegetarian"],
        "[]",
        "[]",
        null,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
