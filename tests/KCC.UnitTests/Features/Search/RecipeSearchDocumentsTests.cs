using KCC.Contributions;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchDocumentsTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task From_RatesTheRecipeAcrossTheVariantsItHolds()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var elsewhere = Guid.NewGuid();
        var stats = ContributionStats.Build([(first.Key, 5m), (second.Key, 3m), (elsewhere, 1m)], []);

        var document = RecipeSearchDocuments.From(Page(Recipe(), first, second), stats, NoNames);

        _ = await Assert.That(document.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(document.ReviewCount).IsEqualTo(2);
    }

    [Test]
    public async Task From_TakesEachTagAndIngredientOnce()
    {
        var first = Variant("Classic Stack", tags: ["Vegan", "Spicy"], ingredientsJson: """[{"name":"Tofu"},{"name":"Chili Oil"}]""");
        var second = Variant("Blueberry Stack", tags: ["Vegan"], ingredientsJson: """[{"name":"Tofu"},{"name":" "}]""");

        var document = RecipeSearchDocuments.From(Page(Recipe(), first, second), ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(string.Join(",", document.Diets)).IsEqualTo("Vegan,Spicy");
        _ = await Assert.That(string.Join(",", document.IngredientNames)).IsEqualTo("Tofu,Chili Oil");
    }

    [Test]
    public async Task From_TimesTheRecipeByItsFastestVariant()
    {
        var page = Page(Recipe(), Variant("Classic Stack", prep: 10, cook: 15), Variant("Blueberry Stack", prep: 5, cook: 5));

        var document = RecipeSearchDocuments.From(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.FastestTime).IsEqualTo(10);
        _ = await Assert.That(document.VariantCount).IsEqualTo(2);
    }

    [Test]
    public async Task From_CarriesTheRecipeCard()
    {
        var priya = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [priya] = "Priya Balan" };

        var document = RecipeSearchDocuments.From(Page(Recipe(priya)), ContributionStats.Build([], []), names);

        _ = await Assert.That(document.Name).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(document.Slug).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/");
        _ = await Assert.That(document.Icon).IsEqualTo("fa-duotone fa-pancakes");
        _ = await Assert.That(document.Category).IsEqualTo("Breakfast");
        _ = await Assert.That(document.Description).IsEqualTo("Tall, tender stacks.");
        _ = await Assert.That(document.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(document.PublishedUnixSeconds).IsEqualTo(1_789_862_400L);
    }

    [Test]
    public async Task From_ReadsADateWithoutAKindAsUtc()
    {
        var document = RecipeSearchDocuments.From(
            Page(Recipe(createDate: new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Unspecified))),
            ContributionStats.Build([], []),
            NoNames);

        _ = await Assert.That(document.PublishedUnixSeconds).IsEqualTo(1_789_862_400L);
    }

    [Test]
    public async Task From_LeavesStartedByEmptyWithoutAnAuthor()
    {
        var document = RecipeSearchDocuments.From(Page(Recipe()), ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.StartedBy).IsEqualTo(string.Empty);
        _ = await Assert.That(document.VariantCount).IsEqualTo(0);
        _ = await Assert.That(document.FastestTime).IsEqualTo(0);
    }

    [Test]
    public async Task From_SkipsIngredientsItCannotRead()
    {
        var page = Page(Recipe(), Variant("Classic Stack", ingredientsJson: "flour, eggs"));

        var document = RecipeSearchDocuments.From(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.IngredientNames.Count).IsEqualTo(0);
        _ = await Assert.That(document.VariantCount).IsEqualTo(1);
    }

    [Test]
    public async Task AuthorKeys_ListsEachAuthorOnceAndSkipsBlanks()
    {
        var priya = Guid.NewGuid();
        var pages = new[] { Page(Recipe(priya)), Page(Recipe()), Page(Recipe(priya)) };

        _ = await Assert.That(string.Join(",", RecipeSearchDocuments.AuthorKeys(pages))).IsEqualTo(priya.ToString());
    }

    private static RecipePageData Page(RecipeRecord recipe, params VariantRecord[] variants) => new(recipe, variants, "/recipes/add-variant/");

    private static RecipeRecord Recipe(Guid? authorKey = null, DateTime? createDate = null) => new(
        Guid.NewGuid(),
        "Fluffy Buttermilk Pancakes",
        "/recipes/fluffy-buttermilk-pancakes/",
        "Tall, tender stacks.",
        "fa-duotone fa-pancakes",
        null,
        "Breakfast",
        authorKey,
        createDate ?? new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    private static VariantRecord Variant(
        string name,
        int prep = 10,
        int cook = 15,
        IReadOnlyList<string> tags = null,
        string ingredientsJson = "[]") => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        null,
        prep,
        cook,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        tags ?? ["Vegetarian"],
        ingredientsJson,
        "[]",
        null,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
