using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.RecipeImport;

public class RecipeCatalogTests
{
    [Test]
    public async Task TheEntries_RunInNameOrder_WithShortIdsFromR1()
    {
        var waffles = Page("Waffles");
        var applePie = Page("apple pie");
        var chili = Page("Chili");

        var catalog = RecipeCatalog.From([waffles, applePie, chili]);

        _ = await Assert.That(catalog.Entries.Select(entry => (entry.ShortId, entry.Name)))
            .IsEquivalentTo([("R1", "apple pie"), ("R2", "Chili"), ("R3", "Waffles")], CollectionOrdering.Matching);
        _ = await Assert.That(catalog.Entries.Select(entry => entry.Key))
            .IsEquivalentTo([applePie.Recipe.Key, chili.Recipe.Key, waffles.Recipe.Key], CollectionOrdering.Matching);
    }

    [Test]
    public async Task TheCatalog_HoldsAtMost500Recipes_TheFirstInNameOrder()
    {
        var pages = Enumerable.Range(1, 501).Reverse().Select(number => Page($"Recipe {number:D3}")).ToList();

        var catalog = RecipeCatalog.From(pages);

        _ = await Assert.That(catalog.Entries.Count).IsEqualTo(500);
        _ = await Assert.That(catalog.Entries[0].Name).IsEqualTo("Recipe 001");
        _ = await Assert.That(catalog.Entries[^1].ShortId).IsEqualTo("R500");
        _ = await Assert.That(catalog.Entries[^1].Name).IsEqualTo("Recipe 500");
    }

    [Test]
    public async Task TheRecipesToPickFrom_AreEveryRecipeGiven_InNameOrder_PastThe500Cap()
    {
        var pages = Enumerable.Range(1, 501).Reverse().Select(number => Page($"Recipe {number:D3}")).ToList();

        var catalog = RecipeCatalog.From(pages);

        _ = await Assert.That(catalog.Choices.Count).IsEqualTo(501);
        _ = await Assert.That(catalog.Choices[0]).IsEqualTo(new RecipeChoice(pages[^1].Recipe.Key, "Recipe 001"));
        _ = await Assert.That(catalog.Choices[^1]).IsEqualTo(new RecipeChoice(pages[0].Recipe.Key, "Recipe 501"));
    }

    [Test]
    public async Task ADescription_IsCutTo160Characters()
    {
        var longDescription = new string('a', 200);

        var catalog = RecipeCatalog.From([Page("Long", longDescription), Page("Short", "Brief."), Page("None", null)]);

        _ = await Assert.That(catalog.Find("R1").Description.Length).IsEqualTo(160);
        _ = await Assert.That(longDescription.StartsWith(catalog.Find("R1").Description, StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(catalog.Find("R2").Description).IsEmpty();
        _ = await Assert.That(catalog.Find("R3").Description).IsEqualTo("Brief.");
    }

    [Test]
    public async Task AnEntry_NamesItsRecipesVariants_InTheirOrder()
    {
        var catalog = RecipeCatalog.From([Page("Tacos", "Taco night.", "Barbacoa", "Black Bean"), Page("Toast")]);

        _ = await Assert.That(catalog.Find("R1").VariantNames)
            .IsEquivalentTo(["Barbacoa", "Black Bean"], CollectionOrdering.Matching);
        _ = await Assert.That(catalog.Find("R2").VariantNames).IsEmpty();
    }

    [Test]
    [Arguments("R0")]
    [Arguments("R3")]
    [Arguments("r1")]
    [Arguments(" R1")]
    [Arguments("1")]
    [Arguments("")]
    [Arguments(null)]
    public async Task AnUnknownId_MapsToNothing(string shortId)
    {
        var catalog = RecipeCatalog.From([Page("Tacos"), Page("Toast")]);

        _ = await Assert.That(catalog.Find(shortId)).IsNull();
        _ = await Assert.That(catalog.Match(shortId, "Same dish.")).IsNull();
    }

    [Test]
    public async Task AKnownId_MapsBackToItsRecipe_WithTheReason()
    {
        var toast = Page("Toast");
        var catalog = RecipeCatalog.From([Page("Tacos"), toast]);

        var match = catalog.Match("R2", "  Same toast, with butter. ");

        _ = await Assert.That(match).IsEqualTo(new RecipeMatch(toast.Recipe.Key, "Toast", "Same toast, with butter."));
    }

    [Test]
    public async Task TheText_HoldsEachEntryOnALineOfItsOwn_AndItsLengthIsTheCharacters()
    {
        var catalog = RecipeCatalog.From([Page("Tacos", "Taco night.", "Barbacoa"), Page("Toast", "Crisp.")]);

        var lines = catalog.Text.Split('\n');

        _ = await Assert.That(lines.Length).IsEqualTo(2);
        _ = await Assert.That(lines[0]).IsEqualTo("""{"id":"R1","name":"Tacos","description":"Taco night.","variants":["Barbacoa"]}""");
        _ = await Assert.That(lines[1]).IsEqualTo("""{"id":"R2","name":"Toast","description":"Crisp.","variants":[]}""");
        _ = await Assert.That(catalog.Characters).IsEqualTo(catalog.Text.Length);
    }

    [Test]
    public async Task AnEmptyCatalog_HasNoTextAndNoCharacters()
    {
        var catalog = RecipeCatalog.From([]);

        _ = await Assert.That(catalog.Entries).IsEmpty();
        _ = await Assert.That(catalog.Text).IsEmpty();
        _ = await Assert.That(catalog.Characters).IsEqualTo(0);
    }

    private static RecipePageData Page(string name, string description = "A recipe.", params string[] variantNames) => new(
        new RecipeRecord(Guid.NewGuid(), name, null, description, null, null, null, null, default),
        [.. variantNames.Select(variantName => Variant(variantName))],
        null);

    private static VariantRecord Variant(string name) => new(
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
        "[]",
        "[]",
        null,
        default);
}
