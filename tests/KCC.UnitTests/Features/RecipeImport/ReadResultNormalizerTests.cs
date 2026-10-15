using System.Globalization;
using KCC.Web.Features.RecipeImport;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.RecipeImport;

public class ReadResultNormalizerTests
{
    private static readonly Mark Shortened = new(MarkKinds.Unclear, null, MarkCodes.Shortened, null);
    private static readonly Mark OutOfRange = new(MarkKinds.Unclear, null, MarkCodes.OutOfRange, null);

    [Test]
    public async Task AReadWithinEveryRule_KeepsItsValuesTrimmed_WithTheModelsMarks()
    {
        var output = Output(
            recipe: new ReaderRecipe(
                new ReaderText("  Pancakes ", null),
                new ReaderText("Fluffy weekend pancakes.", new ReaderMark(MarkKinds.Written, "Not in the photos"))),
            variant: Variant(prep: new ReaderCount(10m, new ReaderMark(MarkKinds.Estimate, "Not printed"))),
            ingredients:
            [
                new ReaderIngredient(" flour ", 1.5m, " cups ", false, null),
                new ReaderIngredient("garlic", 2m, "cloves", false, new ReaderMark(MarkKinds.Unclear, "Printed as 2 to 3")),
                new ReaderIngredient("salt", null, string.Empty, true, null),
            ],
            steps: [new ReaderStep(" Whisk the dry ingredients. ", null)],
            otherRecipes: [" Maple syrup ", " "]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Outcome).IsEqualTo(ReadOutcomes.Recipe);
        _ = await Assert.That(response.Recipe.Name).IsEqualTo(new MarkedValue<string>("Pancakes", null));
        _ = await Assert.That(response.Recipe.Description).IsEqualTo(new MarkedValue<string>(
            "Fluffy weekend pancakes.",
            new Mark(MarkKinds.Written, "Not in the photos", null, null)));
        _ = await Assert.That(response.Variant.PrepTime)
            .IsEqualTo(new MarkedValue<int?>(10, new Mark(MarkKinds.Estimate, "Not printed", null, null)));
        _ = await Assert.That(response.Variant.CookTime).IsEqualTo(new MarkedValue<int?>(15, null));
        _ = await Assert.That(response.Variant.Servings).IsEqualTo(new MarkedValue<int?>(4, null));
        _ = await Assert.That(response.Ingredients).IsEquivalentTo(
            [
                new IngredientValue("flour", 1.5m, "cups", false, null),
                new IngredientValue("garlic", 2m, "cloves", false, new Mark(MarkKinds.Unclear, "Printed as 2 to 3", null, null)),
                new IngredientValue("salt", null, string.Empty, true, null),
            ],
            CollectionOrdering.Matching);
        _ = await Assert.That(response.Instructions).IsEquivalentTo(
            [new InstructionValue("Whisk the dry ingredients.", null)],
            CollectionOrdering.Matching);
        _ = await Assert.That(response.OtherRecipes).IsEquivalentTo(["Maple syrup"], CollectionOrdering.Matching);
        _ = await Assert.That(response.Match).IsNull();
        _ = await Assert.That(response.PossibleDuplicate).IsNull();
    }

    [Test]
    [Arguments("recipe name", 255)]
    [Arguments("recipe description", 2_000)]
    [Arguments("variant name", 255)]
    [Arguments("variant description", 2_000)]
    public async Task AValueAtItsLimit_IsKeptWhole(string field, int limit)
    {
        var text = new string('a', limit);

        var value = TextField(field, ReadResultNormalizer.Normalize(OutputWithText(field, text)));

        _ = await Assert.That(value).IsEqualTo(new MarkedValue<string>(text, null));
    }

    [Test]
    [Arguments("recipe name", 255)]
    [Arguments("recipe description", 2_000)]
    [Arguments("variant name", 255)]
    [Arguments("variant description", 2_000)]
    public async Task AValueOverItsLimit_IsShortenedToIt_AndMarkedUnclearAsShortened(string field, int limit)
    {
        var text = new string('a', limit - 1) + "bc";

        var value = TextField(field, ReadResultNormalizer.Normalize(OutputWithText(field, text)));

        _ = await Assert.That(value).IsEqualTo(new MarkedValue<string>(new string('a', limit - 1) + "b", Shortened));
    }

    [Test]
    public async Task ShorteningNeverSplitsACharacterInTwo()
    {
        var name = new string('a', 254) + "\U0001F95E";

        var response = ReadResultNormalizer.Normalize(Output(recipe: Recipe(name: name)));

        _ = await Assert.That(response.Recipe.Name).IsEqualTo(new MarkedValue<string>(new string('a', 254), Shortened));
    }

    [Test]
    public async Task AnIngredientNameOver200Characters_OrAUnitOver50_IsShortened_AndItsRowMarkedUnclear()
    {
        var output = Output(ingredients:
        [
            new ReaderIngredient(new string('n', 201), 1m, "cup", false, null),
            new ReaderIngredient("sugar", 1m, new string('u', 51), false, null),
            new ReaderIngredient(new string('n', 200), 1m, new string('u', 50), false, null),
        ]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Ingredients).IsEquivalentTo(
            [
                new IngredientValue(new string('n', 200), 1m, "cup", false, Shortened),
                new IngredientValue("sugar", 1m, new string('u', 50), false, Shortened),
                new IngredientValue(new string('n', 200), 1m, new string('u', 50), false, null),
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task AStepOver2000Characters_IsShortened_AndMarkedUnclear()
    {
        var output = Output(steps: [new ReaderStep(new string('s', 2_001), null), new ReaderStep(new string('s', 2_000), null)]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Instructions).IsEquivalentTo(
            [new InstructionValue(new string('s', 2_000), Shortened), new InstructionValue(new string('s', 2_000), null)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task TheListsKeepTheirFirst100IngredientsAndFirst60Steps()
    {
        var output = Output(
            ingredients: [.. Enumerable.Range(1, 101).Select(row => new ReaderIngredient($"item {row}", 1m, "cup", false, null))],
            steps: [.. Enumerable.Range(1, 61).Select(row => new ReaderStep($"step {row}", null))]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Ingredients.Select(ingredient => ingredient.Name)).IsEquivalentTo(
            Enumerable.Range(1, 100).Select(row => $"item {row}"),
            CollectionOrdering.Matching);
        _ = await Assert.That(response.Instructions.Select(step => step.Text)).IsEquivalentTo(
            Enumerable.Range(1, 60).Select(row => $"step {row}"),
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task BlankRows_AreDropped_BeforeTheListsAreCounted()
    {
        var output = Output(
            ingredients:
            [
                .. Enumerable.Range(1, 100).SelectMany(row => new[]
                {
                    new ReaderIngredient("  ", 2m, "cups", false, new ReaderMark(MarkKinds.Unclear, "Smudged")),
                    new ReaderIngredient($"item {row}", 1m, "cup", false, null),
                }),
                null,
            ],
            steps: [new ReaderStep(" ", null), null, new ReaderStep("Mix.", null), new ReaderStep(null, null)]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Ingredients.Select(ingredient => ingredient.Name)).IsEquivalentTo(
            Enumerable.Range(1, 100).Select(row => $"item {row}"),
            CollectionOrdering.Matching);
        _ = await Assert.That(response.Instructions).IsEquivalentTo([new InstructionValue("Mix.", null)], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments("0", 0)]
    [Arguments("45", 45)]
    [Arguments("10080", 10_080)]
    public async Task APrepOrCookTimeOfWholeMinutesFrom0To10080_IsKept(string minutes, int expected)
    {
        var count = new ReaderCount(Number(minutes), null);

        var variant = ReadResultNormalizer.Normalize(Output(variant: Variant(prep: count, cook: count))).Variant;

        _ = await Assert.That(variant.PrepTime).IsEqualTo(new MarkedValue<int?>(expected, null));
        _ = await Assert.That(variant.CookTime).IsEqualTo(new MarkedValue<int?>(expected, null));
    }

    [Test]
    [Arguments("10081")]
    [Arguments("-1")]
    [Arguments("12.5")]
    public async Task APrepOrCookTimeOutsideWholeMinutesFrom0To10080_IsBlanked_AndMarkedUnclearAsOutOfRange(string minutes)
    {
        var count = new ReaderCount(Number(minutes), null);

        var variant = ReadResultNormalizer.Normalize(Output(variant: Variant(prep: count, cook: count))).Variant;

        _ = await Assert.That(variant.PrepTime).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
        _ = await Assert.That(variant.CookTime).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
    }

    [Test]
    [Arguments("1", 1)]
    [Arguments("1000", 1_000)]
    public async Task ServingsFrom1To1000_AreKept(string servings, int expected)
    {
        var variant = ReadResultNormalizer.Normalize(Output(variant: Variant(servings: new ReaderCount(Number(servings), null)))).Variant;

        _ = await Assert.That(variant.Servings).IsEqualTo(new MarkedValue<int?>(expected, null));
    }

    [Test]
    [Arguments("0")]
    [Arguments("1001")]
    [Arguments("2.5")]
    public async Task ServingsOutsideWholeNumbersFrom1To1000_AreBlanked_AndMarkedUnclearAsOutOfRange(string servings)
    {
        var variant = ReadResultNormalizer.Normalize(Output(variant: Variant(servings: new ReaderCount(Number(servings), null)))).Variant;

        _ = await Assert.That(variant.Servings).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
    }

    [Test]
    public async Task AMissingCount_StaysEmpty_WithTheModelsMark()
    {
        var mark = new ReaderMark(MarkKinds.Estimate, "Not printed");

        var variant = ReadResultNormalizer.Normalize(Output(variant: Variant(prep: new ReaderCount(null, mark)))).Variant;

        _ = await Assert.That(variant.PrepTime).IsEqualTo(new MarkedValue<int?>(null, new Mark(MarkKinds.Estimate, "Not printed", null, null)));
    }

    [Test]
    [Arguments("1.5", "1.5")]
    [Arguments("0.3333", "0.333")]
    [Arguments("0.0005", "0.001")]
    [Arguments("2.0004", "2")]
    [Arguments("100000", "100000")]
    [Arguments("100000.0004", "100000")]
    public async Task AQuantityAbove0AndAtMost100000_IsKept_RoundedToThreePlaces(string quantity, string expected)
    {
        var response = ReadResultNormalizer.Normalize(Output(ingredients: [new ReaderIngredient("flour", Number(quantity), "cups", false, null)]));

        _ = await Assert.That(response.Ingredients.Single()).IsEqualTo(new IngredientValue("flour", Number(expected), "cups", false, null));
    }

    [Test]
    [Arguments("0")]
    [Arguments("0.0004")]
    [Arguments("-2")]
    [Arguments("100000.001")]
    public async Task AQuantityThatRoundsTo0OrBelow_OrAbove100000_IsBlanked_AndItsRowBecomesToTaste_MarkedUnclearAsOutOfRange(string quantity)
    {
        var response = ReadResultNormalizer.Normalize(Output(ingredients: [new ReaderIngredient("flour", Number(quantity), "cups", false, null)]));

        _ = await Assert.That(response.Ingredients.Single()).IsEqualTo(new IngredientValue("flour", null, "cups", true, OutOfRange));
    }

    [Test]
    public async Task AnIngredientWithoutAnAmount_BecomesToTaste_KeepingItsMark()
    {
        var smudged = new ReaderMark(MarkKinds.Unclear, "Smudged");

        var response = ReadResultNormalizer.Normalize(Output(ingredients: [new ReaderIngredient("butter", null, "tbsp", false, smudged)]));

        _ = await Assert.That(response.Ingredients.Single())
            .IsEqualTo(new IngredientValue("butter", null, "tbsp", true, new Mark(MarkKinds.Unclear, "Smudged", null, null)));
    }

    [Test]
    public async Task AnIngredientToTaste_IsEyeballed()
    {
        var response = ReadResultNormalizer.Normalize(Output(ingredients: [new ReaderIngredient("pepper", null, null, true, null)]));

        _ = await Assert.That(response.Ingredients.Single()).IsEqualTo(new IngredientValue("pepper", null, string.Empty, true, null));
    }

    [Test]
    public async Task AReadInAnotherLanguage_MarksEveryTextValueWithoutAMarkAsTranslated_WithTheLanguage()
    {
        var output = Output(
            recipe: new ReaderRecipe(new ReaderText("Pancakes", null), new ReaderText("Soft and light.", null)),
            variant: new ReaderVariant(
                new ReaderText("Classic", new ReaderMark(MarkKinds.Written, "Nothing sets it apart")),
                new ReaderText(string.Empty, null),
                new ReaderCount(10m, null),
                new ReaderCount(15m, null),
                new ReaderCount(4m, null)),
            ingredients:
            [
                new ReaderIngredient("flour", 1m, "cup", false, null),
                new ReaderIngredient("lemon zest", 1m, "tsp", false, new ReaderMark(MarkKinds.Unclear, "Smudged")),
            ],
            steps: [new ReaderStep("Whisk everything.", null)],
            sourceLanguage: " Italian ");
        var translated = new Mark(MarkKinds.Translated, null, MarkCodes.Translated, "Italian");

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Recipe.Name.Mark).IsEqualTo(translated);
        _ = await Assert.That(response.Recipe.Description.Mark).IsEqualTo(translated);
        _ = await Assert.That(response.Variant.Name.Mark).IsEqualTo(new Mark(MarkKinds.Written, "Nothing sets it apart", null, null));
        _ = await Assert.That(response.Variant.Description.Mark).IsNull();
        _ = await Assert.That(response.Variant.PrepTime.Mark).IsNull();
        _ = await Assert.That(response.Variant.CookTime.Mark).IsNull();
        _ = await Assert.That(response.Variant.Servings.Mark).IsNull();
        _ = await Assert.That(response.Ingredients.Select(ingredient => ingredient.Mark)).IsEquivalentTo(
            [translated, new Mark(MarkKinds.Unclear, "Smudged", null, null)],
            CollectionOrdering.Matching);
        _ = await Assert.That(response.Instructions.Single().Mark).IsEqualTo(translated);
    }

    [Test]
    [Arguments((string)null)]
    [Arguments("")]
    [Arguments("English")]
    [Arguments("english")]
    public async Task AReadInEnglish_MarksNothingAsTranslated(string sourceLanguage)
    {
        var response = ReadResultNormalizer.Normalize(Output(sourceLanguage: sourceLanguage));

        _ = await Assert.That(response.Recipe.Name.Mark).IsNull();
        _ = await Assert.That(response.Ingredients.Single().Mark).IsNull();
        _ = await Assert.That(response.Instructions.Single().Mark).IsNull();
    }

    [Test]
    public async Task TheUnclearMarkOfAShortenedOrBlankedValue_TakesPrecedenceOverTheModelsEstimateOrWritten()
    {
        var output = Output(
            recipe: Recipe(description: new string('d', 2_001), descriptionMark: new ReaderMark(MarkKinds.Written, "Not in the photos")),
            variant: Variant(servings: new ReaderCount(0m, new ReaderMark(MarkKinds.Estimate, "Not printed"))));

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Recipe.Description.Mark).IsEqualTo(Shortened);
        _ = await Assert.That(response.Variant.Servings).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
    }

    [Test]
    public async Task TheServersUnclearMark_ReplacesTheModelsUnclearMark_OnAValueItChanged()
    {
        var output = Output(ingredients:
        [
            new ReaderIngredient("flour", 0m, "cups", false, new ReaderMark(MarkKinds.Unclear, "Smudged")),
            new ReaderIngredient("sugar", 1m, "cup", false, new ReaderMark(MarkKinds.Unclear, "Smudged")),
        ]);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Ingredients.Select(ingredient => ingredient.Mark)).IsEquivalentTo(
            [OutOfRange, new Mark(MarkKinds.Unclear, "Smudged", null, null)],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task AModelMarkOfAKindTheModelMayNotSet_IsKeptAsUnclear()
    {
        var output = Output(recipe: Recipe(nameMark: new ReaderMark("translated", "From Italian")));

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Recipe.Name.Mark).IsEqualTo(new Mark(MarkKinds.Unclear, "From Italian", null, null));
    }

    [Test]
    public async Task NoMarkTheServerSets_CarriesReasonText_AndNoModelMarkCarriesACode()
    {
        var output = Output(
            recipe: Recipe(name: new string('a', 300), descriptionMark: new ReaderMark(MarkKinds.Written, " Not in the photos ")),
            variant: Variant(prep: new ReaderCount(99_999m, null)),
            ingredients: [new ReaderIngredient("flour", 1m, "cup", false, null)],
            sourceLanguage: "Italian");

        var response = ReadResultNormalizer.Normalize(output);

        var marks = new[] { response.Recipe.Name.Mark, response.Recipe.Description.Mark, response.Variant.PrepTime.Mark }
            .Concat(response.Ingredients.Select(ingredient => ingredient.Mark))
            .ToList();
        _ = await Assert.That(marks.Where(mark => mark.Code is not null).Select(mark => mark.Code)).IsEquivalentTo(
            [MarkCodes.Shortened, MarkCodes.OutOfRange, MarkCodes.Translated],
            CollectionOrdering.Matching);
        _ = await Assert.That(marks.Where(mark => mark.Code is not null).All(mark => mark.Reason is null)).IsTrue();
        _ = await Assert.That(response.Recipe.Description.Mark).IsEqualTo(new Mark(MarkKinds.Written, "Not in the photos", null, null));
    }

    [Test]
    public async Task ANotFoundRead_IsANoRecipeOutcome_WithEveryValueEmpty()
    {
        var response = ReadResultNormalizer.Normalize(Output(found: false, otherRecipes: ["Maple syrup"], sourceLanguage: "Italian"));

        await AssertNoRecipe(response);
    }

    [Test]
    public async Task AFoundReadThatKeepsNoIngredientAndNoStep_IsANoRecipeOutcome_WithEveryValueEmpty()
    {
        var output = Output(
            ingredients: [new ReaderIngredient(" ", 2m, "cups", false, null)],
            steps: [new ReaderStep(string.Empty, null)],
            otherRecipes: ["Maple syrup"]);

        var response = ReadResultNormalizer.Normalize(output);

        await AssertNoRecipe(response);
    }

    [Test]
    public async Task AFoundReadWithAnIngredientOrAStep_IsARecipe()
    {
        var withIngredientsOnly = ReadResultNormalizer.Normalize(Output(steps: []));
        var withStepsOnly = ReadResultNormalizer.Normalize(Output(ingredients: []));

        _ = await Assert.That(withIngredientsOnly.Outcome).IsEqualTo(ReadOutcomes.Recipe);
        _ = await Assert.That(withStepsOnly.Outcome).IsEqualTo(ReadOutcomes.Recipe);
    }

    [Test]
    public async Task MissingPartsOfARead_AreReadAsEmpty()
    {
        var output = new ReaderOutput(
            Found: true,
            Recipe: null,
            Variant: new ReaderVariant(null, new ReaderText(null, null), null, new ReaderCount(null, null), null),
            Ingredients: [new ReaderIngredient("flour", null, null, false, null)],
            Steps: null,
            MatchId: null,
            MatchReason: null,
            OtherRecipes: null,
            SourceLanguage: null);

        var response = ReadResultNormalizer.Normalize(output);

        _ = await Assert.That(response.Outcome).IsEqualTo(ReadOutcomes.Recipe);
        _ = await Assert.That(response.Recipe).IsEqualTo(new RecipeValues(Empty(), Empty()));
        _ = await Assert.That(response.Variant).IsEqualTo(new VariantValues(Empty(), Empty(), NoCount(), NoCount(), NoCount()));
        _ = await Assert.That(response.Ingredients).IsEquivalentTo(
            [new IngredientValue("flour", null, string.Empty, true, null)],
            CollectionOrdering.Matching);
        _ = await Assert.That(response.Instructions).IsEmpty();
        _ = await Assert.That(response.OtherRecipes).IsEmpty();
    }

    private static async Task AssertNoRecipe(ReadResponse response)
    {
        _ = await Assert.That(response.Outcome).IsEqualTo(ReadOutcomes.NoRecipe);
        _ = await Assert.That(response.Recipe).IsEqualTo(new RecipeValues(Empty(), Empty()));
        _ = await Assert.That(response.Variant).IsEqualTo(new VariantValues(Empty(), Empty(), NoCount(), NoCount(), NoCount()));
        _ = await Assert.That(response.Ingredients).IsEmpty();
        _ = await Assert.That(response.Instructions).IsEmpty();
        _ = await Assert.That(response.OtherRecipes).IsEmpty();
        _ = await Assert.That(response.Match).IsNull();
        _ = await Assert.That(response.PossibleDuplicate).IsNull();
    }

    private static MarkedValue<string> Empty() => new(string.Empty, null);

    private static MarkedValue<int?> NoCount() => new(null, null);

    private static decimal Number(string value) => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static ReaderOutput OutputWithText(string field, string text) => field switch
    {
        "recipe name" => Output(recipe: Recipe(name: text)),
        "recipe description" => Output(recipe: Recipe(description: text)),
        "variant name" => Output(variant: Variant(name: text)),
        "variant description" => Output(variant: Variant(description: text)),
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    private static MarkedValue<string> TextField(string field, ReadResponse response) => field switch
    {
        "recipe name" => response.Recipe.Name,
        "recipe description" => response.Recipe.Description,
        "variant name" => response.Variant.Name,
        "variant description" => response.Variant.Description,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    private static ReaderOutput Output(
        ReaderRecipe recipe = null,
        ReaderVariant variant = null,
        IReadOnlyList<ReaderIngredient> ingredients = null,
        IReadOnlyList<ReaderStep> steps = null,
        bool found = true,
        IReadOnlyList<string> otherRecipes = null,
        string sourceLanguage = null) =>
        new(
            found,
            recipe ?? Recipe(),
            variant ?? Variant(),
            ingredients ?? [new ReaderIngredient("flour", 1.5m, "cups", false, null)],
            steps ?? [new ReaderStep("Whisk the dry ingredients.", null)],
            null,
            null,
            otherRecipes ?? [],
            sourceLanguage);

    private static ReaderRecipe Recipe(
        string name = "Pancakes",
        string description = "Fluffy weekend pancakes.",
        ReaderMark nameMark = null,
        ReaderMark descriptionMark = null) =>
        new(new ReaderText(name, nameMark), new ReaderText(description, descriptionMark));

    private static ReaderVariant Variant(
        string name = "Buttermilk",
        string description = "Tangy and tall.",
        ReaderCount prep = null,
        ReaderCount cook = null,
        ReaderCount servings = null) =>
        new(
            new ReaderText(name, null),
            new ReaderText(description, null),
            prep ?? new ReaderCount(10m, null),
            cook ?? new ReaderCount(15m, null),
            servings ?? new ReaderCount(4m, null));
}
