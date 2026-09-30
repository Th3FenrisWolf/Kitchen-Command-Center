using KCC.Contributions;
using KCC.Web.Features.Pages.RecipeDetail;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.Pages.RecipeDetail;

public class RecipeDetailMappingTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task Map_RatesTheRecipeAcrossTheVariantsItLists()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var elsewhere = Guid.NewGuid();
        var stats = ContributionStats.Build([(first.Key, 5m), (second.Key, 3m), (elsewhere, 1m)], []);

        var viewModel = RecipeDetailMapping.Map(Page(first, second), stats, NoNames);

        _ = await Assert.That(viewModel.RecipeAverageRating).IsEqualTo(4d);
        _ = await Assert.That(viewModel.RecipeReviewCount).IsEqualTo(2);
    }

    [Test]
    public async Task Map_SumsTheTimesCookedAcrossItsVariants()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var stats = ContributionStats.Build([], [first.Key, second.Key, second.Key]);

        var viewModel = RecipeDetailMapping.Map(Page(first, second), stats, NoNames);

        _ = await Assert.That(viewModel.RecipeTimesCooked).IsEqualTo(3);
    }

    [Test]
    public async Task Map_NamesTheRecipeAndVariantAuthors()
    {
        var priya = Guid.NewGuid();
        var diego = Guid.NewGuid();
        var page = Page(Variant("Classic Stack", authorKey: diego)) with { Recipe = Recipe(authorKey: priya) };
        var names = new Dictionary<Guid, string> { [priya] = "Priya Balan", [diego] = "Diego Salazar" };

        var viewModel = RecipeDetailMapping.Map(page, ContributionStats.Build([], []), names);

        _ = await Assert.That(viewModel.StartedByName).IsEqualTo("Priya Balan");
        _ = await Assert.That(viewModel.Variants.Single().AuthorName).IsEqualTo("Diego Salazar");
    }

    [Test]
    public async Task Map_CarriesEachVariantsCard()
    {
        var variant = Variant("Classic Stack", imageUrl: "/media/stack.jpg?width=192");
        var stats = ContributionStats.Build([(variant.Key, 4.5m), (variant.Key, 4m)], [variant.Key]);

        var card = RecipeDetailMapping.Map(Page(variant), stats, NoNames).Variants.Single();

        _ = await Assert.That(card.Name).IsEqualTo("Classic Stack");
        _ = await Assert.That(card.Slug).IsEqualTo(variant.Url);
        _ = await Assert.That(card.Image).IsEqualTo("/media/stack.jpg?width=192");
        _ = await Assert.That(card.TotalTime).IsEqualTo(25);
        _ = await Assert.That(card.PublishedDate).IsEqualTo(variant.CreateDate);
        _ = await Assert.That(card.AverageRating).IsEqualTo(4.25d);
        _ = await Assert.That(card.ReviewCount).IsEqualTo(2);
        _ = await Assert.That(card.CookedCount).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", card.Tags)).IsEqualTo("Vegetarian");
    }

    [Test]
    public async Task Map_CarriesTheRecipeHeader()
    {
        var page = Page();

        var viewModel = RecipeDetailMapping.Map(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(viewModel.RecipeName).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(viewModel.RecipeCategory).IsEqualTo("Breakfast");
        _ = await Assert.That(viewModel.RecipeGuid).IsEqualTo(page.Recipe.Key);
        _ = await Assert.That(viewModel.AddVariantUrl).IsEqualTo("/recipes/add-variant/");
    }

    [Test]
    public async Task AuthorKeys_TakesTheRecipeAndVariantAuthorsAndSkipsBlanks()
    {
        var priya = Guid.NewGuid();
        var diego = Guid.NewGuid();
        var page = Page(Variant("A", authorKey: diego), Variant("B")) with { Recipe = Recipe(authorKey: priya) };

        _ = await Assert.That(string.Join(",", RecipeDetailMapping.AuthorKeys(page))).IsEqualTo($"{priya},{diego}");
    }

    private static RecipePageData Page(params VariantRecord[] variants) => new(Recipe(), variants, "/recipes/add-variant/");

    private static RecipeRecord Recipe(Guid? authorKey = null) => new(
        Guid.NewGuid(),
        "Fluffy Buttermilk Pancakes",
        "/recipes/fluffy-buttermilk-pancakes/",
        "Tall, tender stacks.",
        "fa-duotone fa-pancakes",
        null,
        "Breakfast",
        authorKey,
        new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    private static VariantRecord Variant(string name, Guid? authorKey = null, string imageUrl = null) => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        imageUrl,
        10,
        15,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        ["Vegetarian"],
        "[]",
        "[]",
        authorKey,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
