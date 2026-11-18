using KCC.ReaderEval;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.Configuration;

namespace KCC.UnitTests.Tools;

public class ReaderEvalSetupTests
{
    private const string Key = "sk-ant-test-0123456789";

    private static readonly string[] FourModels = ["claude-haiku-5-5", "claude-haiku-4-5", "claude-sonnet-5-5", "claude-opus-5-5"];

    [Test]
    public async Task Check_CountsForTheFourModels_AtTheDefaultEdge()
    {
        var parsed = EvalOptions.TryParse(["--check"], out var options, out _);

        _ = await Assert.That(parsed).IsTrue();
        _ = await Assert.That(options.Mode).IsEqualTo(EvalMode.Check);
        _ = await Assert.That(options.Models).IsEquivalentTo(FourModels, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(options.Edge).IsEqualTo(new RecipeImportOptions().MaxImageEdge);
    }

    [Test]
    public async Task Draft_ReadsWithTheStrongestModel()
    {
        var parsed = EvalOptions.TryParse(["--draft"], out var options, out _);

        _ = await Assert.That(parsed).IsTrue();
        _ = await Assert.That(options.Mode).IsEqualTo(EvalMode.Draft);
        _ = await Assert.That(options.Models).IsEquivalentTo(["claude-opus-5-5"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(options.Edge).IsEqualTo(2000);
    }

    [Test]
    public async Task Models_ComparesTheNamedModelsInTheirOrder_AtTheNamedEdge()
    {
        var parsed = EvalOptions.TryParse(["--models", "claude-sonnet-5-5,claude-haiku-5-5", "--edge", "1568"], out var options, out _);

        _ = await Assert.That(parsed).IsTrue();
        _ = await Assert.That(options.Mode).IsEqualTo(EvalMode.Compare);
        _ = await Assert.That(options.Models)
            .IsEquivalentTo(["claude-sonnet-5-5", "claude-haiku-5-5"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(options.Edge).IsEqualTo(1568);
    }

    [Test]
    [Arguments("")]
    [Arguments("--check --draft")]
    [Arguments("--models")]
    [Arguments("--models claude-opus-4-1")]
    [Arguments("--models claude-haiku-5-5,claude-haiku-5-5")]
    [Arguments("--draft --models claude-haiku-5-5,claude-opus-5-5")]
    [Arguments("--models claude-haiku-5-5 --edge 0")]
    [Arguments("--models claude-haiku-5-5 --edge wide")]
    [Arguments("--models claude-haiku-5-5 --edge")]
    [Arguments("--check --verbose")]
    [Arguments("--check --check")]
    public async Task ArgumentsThatNameNoRunOrABadValue_AreRefusedWithAReason(string arguments)
    {
        var parsed = EvalOptions.TryParse(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), out _, out var error);

        _ = await Assert.That(parsed).IsFalse();
        _ = await Assert.That(string.IsNullOrWhiteSpace(error)).IsFalse();
    }

    [Test]
    public async Task TheKey_ComesFromKccWebsUserSecretsFirst()
    {
        var key = AnthropicKey.Find(Secrets(Key), name => name == AnthropicKey.EnvironmentVariable ? "sk-ant-other" : null);

        _ = await Assert.That(key.Value).IsEqualTo(Key);
        _ = await Assert.That(key.Source.Contains("user-secrets", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task TheKey_ComesFromAnthropicApiKey_WhenTheUserSecretsHoldNone()
    {
        var key = AnthropicKey.Find(Secrets("  "), name => name == "ANTHROPIC_API_KEY" ? Key : null);

        _ = await Assert.That(key.Value).IsEqualTo(Key);
        _ = await Assert.That(key.Source).IsEqualTo("ANTHROPIC_API_KEY");
    }

    [Test]
    public async Task NoKey_IsFound_WhenNeitherHoldsOne()
    {
        var key = AnthropicKey.Find(Secrets(null), _ => string.Empty);

        _ = await Assert.That(key).IsNull();
    }

    [Test]
    public async Task TheKeysText_NamesItsSourceAndNeverTheKey()
    {
        var key = AnthropicKey.Find(Secrets(Key), _ => null);

        _ = await Assert.That(key.ToString().Contains(Key, StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That($"{key}".Contains(Key, StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task AMissingSet_IsAProblemThatSaysWhereToPutTheCases()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"kcc-readereval-missing-{Guid.NewGuid():N}");

        var cases = EvalSet.Load(missing, out var problem);

        _ = await Assert.That(cases).IsEmpty();
        _ = await Assert.That(problem.Contains(missing, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task AnEmptySet_IsAProblemThatSaysWhereToPutTheCases()
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            File.WriteAllText(Path.Combine(set.FullName, ".DS_Store"), string.Empty);

            var cases = EvalSet.Load(set.FullName, out var problem);

            _ = await Assert.That(cases).IsEmpty();
            _ = await Assert.That(problem.Contains(set.FullName, StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ACase_HoldsItsPhotosInPageOrder_AndLeavesOutHiddenFiles()
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            var photos = Directory.CreateDirectory(Path.Combine(set.FullName, "printed-pancakes", "photos"));
            foreach (var name in new[] { "10.jpg", "2.jpg", "1.jpg", ".DS_Store" })
            {
                File.WriteAllText(Path.Combine(photos.FullName, name), string.Empty);
            }

            File.WriteAllText(Path.Combine(set.FullName, "catalog.json"), "[]");
            Directory.CreateDirectory(Path.Combine(set.FullName, "handwritten-chili", "photos"));
            File.WriteAllText(Path.Combine(set.FullName, "handwritten-chili", "photos", "card.jpg"), string.Empty);

            var cases = EvalSet.Load(set.FullName, out var problem);

            _ = await Assert.That(problem).IsNull();
            _ = await Assert.That(cases.Select(evalCase => evalCase.Name))
                .IsEquivalentTo(["handwritten-chili", "printed-pancakes"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(cases[1].Photos.Select(Path.GetFileName))
                .IsEquivalentTo(["1.jpg", "2.jpg", "10.jpg"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(cases[1].ExpectedPath)
                .IsEqualTo(Path.Combine(set.FullName, "printed-pancakes", "expected.json"));
            _ = await Assert.That(cases[1].DraftPath)
                .IsEqualTo(Path.Combine(set.FullName, "printed-pancakes", "expected.draft.json"));
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ACaseWithoutPhotos_IsAProblemThatNamesIt()
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            Directory.CreateDirectory(Path.Combine(set.FullName, "printed-pancakes", "photos"));

            var cases = EvalSet.Load(set.FullName, out var problem);

            _ = await Assert.That(cases).IsEmpty();
            _ = await Assert.That(problem.Contains("printed-pancakes", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    public async Task AnExpectedAnswer_RoundTripsThroughItsJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        var answer = new ExpectedAnswer(
            Handwritten: true,
            new ExpectedRecipe(new("Chili", true), new("A hearty chili.", false)),
            new ExpectedVariant(new("Classic", false), new(string.Empty, true), new(null, true), new(90, true), new(6, false)),
            [new ExpectedIngredient("beef", 2m, "lb"), new ExpectedIngredient("chili powder", 0.333m, "T")],
            ["Brown the beef.", "Simmer for 90 minutes."]);
        try
        {
            answer.Save(path);
            var loaded = ExpectedAnswer.Load(path);

            _ = await Assert.That(loaded.Handwritten).IsTrue();
            _ = await Assert.That(loaded.Recipe).IsEqualTo(answer.Recipe);
            _ = await Assert.That(loaded.Variant).IsEqualTo(answer.Variant);
            _ = await Assert.That(loaded.Ingredients).IsEquivalentTo(answer.Ingredients, TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(loaded.Steps).IsEquivalentTo(answer.Steps, TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That((await File.ReadAllTextAsync(path)).Contains("\"stated\": false", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ASetWithoutACatalog_HasNoCatalog_AndNoProblem()
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            var catalog = EvalSet.LoadCatalog(set.FullName, out var problem);

            _ = await Assert.That(catalog).IsNull();
            _ = await Assert.That(problem).IsNull();
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    public async Task TheSetsCatalog_HoldsItsRecipesInNameOrder_WithTheirDescriptionsAndVariantNames()
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            File.WriteAllText(
                Path.Combine(set.FullName, "catalog.json"),
                """
                [
                  { "name": "Weeknight Tacos", "description": "Taco night.", "variants": ["Slow Beef Barbacoa", "Quick Black Bean"] },
                  { "name": "Golden Milk", "description": "Warm, don't boil.", "variants": [] }
                ]
                """);

            var catalog = EvalSet.LoadCatalog(set.FullName, out var problem);

            _ = await Assert.That(problem).IsNull();
            _ = await Assert.That(catalog.Entries.Select(entry => (entry.ShortId, entry.Name, entry.Description)))
                .IsEquivalentTo(
                    [("R1", "Golden Milk", "Warm, don't boil."), ("R2", "Weeknight Tacos", "Taco night.")],
                    TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(catalog.Find("R2").VariantNames)
                .IsEquivalentTo(["Slow Beef Barbacoa", "Quick Black Bean"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    [Arguments("{ \"name\": \"Tacos\" }")]
    [Arguments("[{ \"description\": \"No name.\" }]")]
    [Arguments("not json")]
    public async Task ACatalogThatCannotBeRead_IsAProblemThatNamesIt(string json)
    {
        var set = Directory.CreateTempSubdirectory("kcc-readereval-");
        try
        {
            File.WriteAllText(Path.Combine(set.FullName, "catalog.json"), json);

            var catalog = EvalSet.LoadCatalog(set.FullName, out var problem);

            _ = await Assert.That(catalog).IsNull();
            _ = await Assert.That(problem.Contains("catalog.json", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            set.Delete(recursive: true);
        }
    }

    [Test]
    public async Task AnExpectedAnswer_KeepsItsMatch_AndOneWithoutAMatchLoadsWithNone()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        var answer = new ExpectedAnswer(
            Handwritten: false,
            new ExpectedRecipe(new("Tacos", true), new("Taco night.", true)),
            new ExpectedVariant(new("Fish", true), new("Crisp.", true), new(10, true), new(10, true), new(4, true)),
            [new ExpectedIngredient("cod", 1m, "lb")],
            ["Fry the fish."],
            Match: "Weeknight Tacos");
        try
        {
            answer.Save(path);
            var loaded = ExpectedAnswer.Load(path);
            var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
            _ = json.Remove("match");
            await File.WriteAllTextAsync(path, json.ToJsonString());
            var withoutMatch = ExpectedAnswer.Load(path);

            _ = await Assert.That(loaded.Match).IsEqualTo("Weeknight Tacos");
            _ = await Assert.That(withoutMatch.Match).IsNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task AnExpectedIngredient_KeepsItsAcceptedSpellings_AndOneWithoutThemLoadsWithNone()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        var answer = new ExpectedAnswer(
            Handwritten: true,
            new ExpectedRecipe(new("Pizza", true), new("Friday pizza.", true)),
            new ExpectedVariant(new("Classic", true), new(string.Empty, true), new(20, true), new(12, true), new(4, true)),
            [new ExpectedIngredient("mozz cheese", 2m, "cups", ["mozzarella cheese"]), new ExpectedIngredient("flour", 3m, "cups")],
            ["Bake the pizza."]);
        try
        {
            answer.Save(path);
            var json = await File.ReadAllTextAsync(path);
            var loaded = ExpectedAnswer.Load(path);
            var older = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
            _ = older["ingredients"]![0]!.AsObject().Remove("accepted");
            await File.WriteAllTextAsync(path, older.ToJsonString());
            var withoutAccepted = ExpectedAnswer.Load(path);

            _ = await Assert.That(loaded.Ingredients[0].Accepted).IsEquivalentTo(["mozzarella cheese"]);
            _ = await Assert.That(json.Split("\"accepted\"").Length - 1).IsEqualTo(1);
            _ = await Assert.That(json.Contains("\"names\"", StringComparison.Ordinal)).IsFalse();
            _ = await Assert.That(withoutAccepted.Ingredients.All(line => line.Accepted is null || line.Accepted.Count == 0)).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task AnExpectedAnswer_KeepsItsAcceptedSteps_AndOneWithoutThemLoadsWithNone()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        var answer = new ExpectedAnswer(
            Handwritten: true,
            new ExpectedRecipe(new("Mushrooms", true), new(string.Empty, true)),
            new ExpectedVariant(new("Buttered", true), new(string.Empty, true), new(5, true), new(5, true), new(2, true)),
            [new ExpectedIngredient("mushrooms", 1m, "lb")],
            ["Sauté mushrooms in butter (5 min) with salt and pepper.", "Serve hot."],
            AcceptedSteps: new Dictionary<int, IReadOnlyList<string>> { [1] = ["Sautee mushrooms in butter (5 min) w/salt + pepper."] });
        try
        {
            answer.Save(path);
            var loaded = ExpectedAnswer.Load(path);
            var older = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
            _ = older.Remove("acceptedSteps");
            await File.WriteAllTextAsync(path, older.ToJsonString());
            var withoutAccepted = ExpectedAnswer.Load(path);

            _ = await Assert.That(loaded.Steps).IsEquivalentTo(answer.Steps, TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(loaded.AcceptedSteps[1]).IsEquivalentTo(["Sautee mushrooms in butter (5 min) w/salt + pepper."]);
            _ = await Assert.That(withoutAccepted.AcceptedSteps).IsNull();
            _ = await Assert.That(withoutAccepted.Steps).IsEquivalentTo(answer.Steps, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ADraftedAnswer_WritesNoAcceptedSpellingsOrSteps()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        var read = new ReadResponse(
            ReadOutcomes.Recipe,
            new RecipeValues(new("Pizza", null), new("Friday pizza.", null)),
            new VariantValues(new("Classic", null), new(string.Empty, null), new(20, null), new(12, null), new(4, null)),
            [new IngredientValue("mozz cheese", 2m, "cups", false, null)],
            [new InstructionValue("Bake the pizza.", null)],
            Match: null,
            PossibleDuplicate: null,
            OtherRecipes: [],
            Recipes: null);
        try
        {
            ExpectedAnswer.FromRead(read, handwritten: true).Save(path);
            var json = await File.ReadAllTextAsync(path);

            _ = await Assert.That(json.Contains("\"accepted\"", StringComparison.Ordinal)).IsFalse();
            _ = await Assert.That(json.Contains("\"acceptedSteps\"", StringComparison.Ordinal)).IsFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ExpectedJson_LoadsTheDetailsBlockOfTheQuickstart()
    {
        const string json = """
            {
              "handwritten": false,
              "recipe": { "name": { "value": "Cake", "stated": true }, "description": { "value": "", "stated": false } },
              "variant": {
                "name": { "value": "", "stated": false }, "description": { "value": "", "stated": false },
                "prepMinutes": { "value": null, "stated": false }, "cookMinutes": { "value": null, "stated": false },
                "servings": { "value": null, "stated": false }
              },
              "ingredients": [], "steps": [], "match": null,
              "details": {
                "category": "Dinner",
                "tags": [{ "name": "Vegan", "stated": true }, { "name": "Spicy", "stated": false }],
                "difficulty": null,
                "nutritionPrinted": "converted",
                "nutrition": { "calories": 450, "proteinG": 12, "carbsG": 60, "fatG": 14,
                               "saturatedFatG": 2, "fiberG": 8, "sugarG": 9, "sodiumMg": null },
                "draft": true
              }
            }
            """;
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        try
        {
            var details = ExpectedAnswer.Load(path).Details;

            _ = await Assert.That(details.Category).IsEqualTo("Dinner");
            _ = await Assert.That(details.Tags.Select(tag => (tag.Name, tag.Stated)))
                .IsEquivalentTo([("Vegan", true), ("Spicy", false)]);
            _ = await Assert.That(details.Difficulty).IsNull();
            _ = await Assert.That(details.NutritionPrinted).IsEqualTo("converted");
            _ = await Assert.That(details.Nutrition.Calories).IsEqualTo(450);
            _ = await Assert.That(details.Nutrition.SodiumMg).IsNull();
            _ = await Assert.That(details.Draft).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ExpectedJson_WithoutDetails_LoadsWithNoDetails()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        try
        {
            Answer().Save(path);

            _ = await Assert.That(ExpectedAnswer.Load(path).Details).IsNull();
            _ = await Assert.That((await File.ReadAllTextAsync(path)).Contains("details", StringComparison.Ordinal)).IsFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ADraftedAnswer_DraftsPerServing_WhenOnlyRoundingIsCalculated()
    {
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null),
            [],
            new MarkedValue<string>(null, null),
            new NutritionValues(
                new(450, null),
                new(2, new Mark(MarkKinds.Calculated, null, null, "1.5 g")),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(500, new Mark(MarkKinds.Calculated, null, null, "0.5 g")))));

        var details = ExpectedAnswer.FromRead(read, handwritten: false).Details;

        _ = await Assert.That(details.NutritionPrinted).IsEqualTo(ExpectedDetails.PerServing);
    }

    [Test]
    public async Task ADraftedAnswer_DraftsConverted_WhenTheCaloriesWerePrintedInKilojoules_InAnyCase()
    {
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null),
            [],
            new MarkedValue<string>(null, null),
            new NutritionValues(
                new(450, new Mark(MarkKinds.Calculated, null, null, "1883 KJ")),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null))));

        var details = ExpectedAnswer.FromRead(read, handwritten: false).Details;

        _ = await Assert.That(details.NutritionPrinted).IsEqualTo(ExpectedDetails.Converted);
    }

    [Test]
    public async Task ADraftedAnswer_DraftsConverted_WhenThePanelIsPerWholeRecipe_EvenInKilocalories()
    {
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null),
            [],
            new MarkedValue<string>(null, null),
            new NutritionValues(
                new(500, new Mark(MarkKinds.Calculated, null, MarkCodes.PrintedAs, "2000 kcal")),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null))));
        ReaderDetails raw = new(
            null,
            [],
            null,
            new ReaderNutrition("recipe", [new ReaderNutrient("calories", 2000m, "kcal", "2000 kcal", false)]));

        var details = ExpectedAnswer.FromRead(read, handwritten: false, raw).Details;

        _ = await Assert.That(details.NutritionPrinted).IsEqualTo(ExpectedDetails.Converted);
    }

    [Test]
    public async Task ADraftedAnswer_DraftsStatedFalse_ForAProposedTag_WhoseModelMarkIsAnEstimate()
    {
        var taxonomy = EvalTaxonomy.Create();
        var spicy = TaxonomyDefaults.Values.Single(value => value.Name == "Spicy").Key;
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null),
            [new TagValue(spicy, new Mark(MarkKinds.Estimate, "Looks hot", null, null))],
            new MarkedValue<string>(null, null),
            null));
        ReaderDetails raw = new(
            null,
            [new ReaderTag(taxonomy.TagId("Spicy"), false, null, new ReaderMark("estimate", "Looks hot"))],
            null,
            null);

        var details = ExpectedAnswer.FromRead(read, handwritten: false, raw).Details;

        _ = await Assert.That(details.Tags.Select(tag => (tag.Name, tag.Stated))).IsEquivalentTo([("Spicy", false)]);
    }

    [Test]
    public async Task ADraftedAnswer_DraftsStatedTrue_ForATagThePhotosName()
    {
        var taxonomy = EvalTaxonomy.Create();
        var vegan = TaxonomyDefaults.Values.Single(value => value.Name == "Vegan").Key;
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null),
            [new TagValue(vegan, new Mark(MarkKinds.Estimate, null, null, null))],
            new MarkedValue<string>(null, null),
            null));
        ReaderDetails raw = new(
            null,
            [new ReaderTag(taxonomy.TagId("Vegan"), true, "Vegan", new ReaderMark("estimate", null))],
            null,
            null);

        var details = ExpectedAnswer.FromRead(read, handwritten: false, raw).Details;

        _ = await Assert.That(details.Tags.Select(tag => (tag.Name, tag.Stated))).IsEquivalentTo([("Vegan", true)]);
    }

    [Test]
    public async Task ADraftedAnswer_DraftsNoNutritionPrinted_WhenNoNutrientHasAValue()
    {
        var details = ExpectedAnswer.FromRead(ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(null, null), [], new MarkedValue<string>(null, null), null)), handwritten: false).Details;

        _ = await Assert.That(details.NutritionPrinted).IsNull();
    }

    [Test]
    public async Task ADraftedAnswer_WritesADetailsBlock_CarryingDraftTrue()
    {
        var vegan = TaxonomyDefaults.Values.Single(value => value.Name == "Vegan").Key;
        var spicy = TaxonomyDefaults.Values.Single(value => value.Name == "Spicy").Key;
        var dinner = TaxonomyDefaults.Values.Single(value => value.Name == "Dinner").Key;
        var read = ReadWith(new DetailsValues(
            new MarkedValue<Guid?>(dinner, new Mark(MarkKinds.Suggested, "Hearty", null, null)),
            [new TagValue(vegan, null), new TagValue(spicy, new Mark(MarkKinds.Suggested, null, null, null))],
            new MarkedValue<string>("Easy", new Mark(MarkKinds.Suggested, null, null, null)),
            new NutritionValues(
                new(450, new Mark(MarkKinds.Calculated, null, MarkCodes.PrintedAs, "1883 kJ")),
                new(12, new Mark(MarkKinds.Calculated, null, null, null)),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null),
                new(null, null))));
        var path = Path.Combine(Path.GetTempPath(), $"kcc-readereval-{Guid.NewGuid():N}.json");
        try
        {
            var taxonomy = EvalTaxonomy.Create();
            ReaderDetails raw = new(
                null,
                [
                    new ReaderTag(taxonomy.TagId("Vegan"), true, "Vegan", null),
                    new ReaderTag(taxonomy.TagId("Spicy"), false, null, new ReaderMark("suggested", "Hot")),
                ],
                null,
                null);
            ExpectedAnswer.FromRead(read, handwritten: false, raw).Save(path);
            var details = ExpectedAnswer.Load(path).Details;

            _ = await Assert.That(details.Draft).IsTrue();
            _ = await Assert.That((await File.ReadAllTextAsync(path)).Contains("\"draft\": true", StringComparison.Ordinal)).IsTrue();
            _ = await Assert.That(details.Category).IsEqualTo("Dinner");
            _ = await Assert.That(details.Tags.Select(tag => (tag.Name, tag.Stated)))
                .IsEquivalentTo([("Vegan", true), ("Spicy", false)]);
            _ = await Assert.That(details.Difficulty).IsNull();
            _ = await Assert.That(details.NutritionPrinted).IsEqualTo("converted");
            _ = await Assert.That(details.Nutrition.Calories).IsEqualTo(450);
            _ = await Assert.That(details.Nutrition.ProteinG).IsEqualTo(12);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task TheTool_SendsTaxonomyDefaultsThroughReaderTaxonomy()
    {
        var taxonomy = EvalTaxonomy.Create();

        _ = await Assert.That(taxonomy.Characters).IsGreaterThan(0);
        foreach (var value in TaxonomyDefaults.Values)
        {
            _ = await Assert.That(taxonomy.Text.Contains($"\"name\":\"{value.Name}\"", StringComparison.Ordinal)).IsTrue();
        }

        _ = await Assert.That(taxonomy.Text.Contains("\"name\":\"Vegan\",\"group\":\"diet\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(taxonomy.Text.Contains("\"name\":\"Spicy\",\"group\":\"style\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(taxonomy.Category("C1")?.Name).IsEqualTo("Breakfast");
    }

    [Test]
    public async Task Models_RefusesBeforeAnySpend_OnTheDetailsGateAlone()
    {
        using var set = new GateSet();
        var service = new CountingService();
        var asked = false;
        var output = new StringWriter();
        var runner = new EvalRunner(service, output, _ => asked = true, GateSet.CatalogWithCake(), EvalTaxonomy.Create());
        var cases = set.Cases(total: 10, categories: 10, nutrition: 3, converted: 0, judgeable: true);

        var comparison = await runner.CompareAsync(cases, ["claude-haiku-5-5"], 2000, CancellationToken.None);

        var text = output.ToString();
        _ = await Assert.That(comparison).IsNull();
        _ = await Assert.That(service.Calls).IsEqualTo(0);
        _ = await Assert.That(asked).IsFalse();
        _ = await Assert.That(text.Contains("nutritionPrinted \"converted\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(text.Contains("cannot be judged", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task DetailsProblems_IsEmpty_ForASetThatMeetsEveryRule()
    {
        _ = await Assert.That(EvalSet.DetailsProblems(Passing())).IsEmpty();
    }

    [Test]
    public async Task DetailsProblems_NamesACaseWithNoDetailsBlock()
    {
        var answers = Passing();
        answers["case-0"] = Answer();

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Count(problem => problem.Contains("case-0 has no details block", StringComparison.Ordinal))).IsEqualTo(1);
    }

    [Test]
    public async Task DetailsProblems_NamesACaseWhoseBlockIsStillADraft()
    {
        var answers = Passing();
        answers["case-1"] = Answer(answers["case-1"].Details with { Draft = true });

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Count(problem => problem.Contains("case-1: its details block is still a draft", StringComparison.Ordinal))).IsEqualTo(1);
    }

    [Test]
    public async Task DetailsProblems_RefusesFewerThanThreeCasesExpectingNutrition()
    {
        var answers = Passing(nutrition: 2);

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Single()).IsEqualTo("  2 cases expect a nutrition value, and the comparison needs at least 3.");
    }

    [Test]
    public async Task DetailsProblems_RefusesASetWithNoConvertedCase()
    {
        var answers = Passing(converted: 0);

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Single().Contains("nutritionPrinted \"converted\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task DetailsProblems_RefusesFewerThanTenCasesExpectingACategory()
    {
        var answers = Passing(categories: 9);

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Single()).IsEqualTo("  9 cases expect a category, and the comparison needs at least 10.");
    }

    [Test]
    public async Task DetailsProblems_NamesACaseWhoseNutritionIsNull_WithoutThrowing()
    {
        var answers = Passing();
        answers["case-5"] = Answer(answers["case-5"].Details with { Nutrition = null });

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Count(problem => problem.Contains("case-5", StringComparison.Ordinal) && problem.Contains("nutrition", StringComparison.Ordinal))).IsEqualTo(1);
    }

    [Test]
    public async Task DetailsProblems_NamesAnUnknownCategoryOrTag()
    {
        var answers = Passing();
        answers["case-2"] = Answer(answers["case-2"].Details with { Category = "Dinnr", Tags = [new ExpectedTag("Vegan", true), new ExpectedTag("Spcy", false)] });

        var problems = EvalSet.DetailsProblems(answers);

        _ = await Assert.That(problems.Count(problem => problem.Contains("case-2", StringComparison.Ordinal) && problem.Contains("Dinnr", StringComparison.Ordinal))).IsEqualTo(1);
        _ = await Assert.That(problems.Count(problem => problem.Contains("case-2", StringComparison.Ordinal) && problem.Contains("Spcy", StringComparison.Ordinal))).IsEqualTo(1);
        _ = await Assert.That(problems.Any(problem => problem.Contains("Vegan", StringComparison.Ordinal))).IsFalse();
    }

    private static Dictionary<string, ExpectedAnswer> Passing(int categories = 10, int nutrition = 3, int converted = 1) =>
        Enumerable.Range(0, 10).ToDictionary(
            index => $"case-{index}",
            index => Answer(new ExpectedDetails(
                index < categories ? "Dinner" : null,
                [],
                null,
                index < nutrition ? (index < converted ? ExpectedDetails.Converted : ExpectedDetails.PerServing) : null,
                index < nutrition
                    ? new ExpectedNutrition(450, null, null, null, null, null, null, null)
                    : new ExpectedNutrition(null, null, null, null, null, null, null, null))));

    [Test]
    public async Task Models_NamesWhatTheSetLacks()
    {
        using var set = new GateSet();
        var output = new StringWriter();
        var runner = new EvalRunner(new CountingService(), output, _ => true, GateSet.Catalog(), EvalTaxonomy.Create());

        _ = await runner.CompareAsync(set.Cases(10, 9, 2, 0, draft: true), ["claude-haiku-5-5"], 2000, CancellationToken.None);

        var text = output.ToString();
        _ = await Assert.That(text.Contains("draft", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(text.Contains("nutrition", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(text.Contains("kilojoules", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(text.Contains("category", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task TheModelsUsage_StatesTheDetailsGate()
    {
        _ = await Assert.That(EvalOptions.Usage.Contains("details", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(EvalOptions.Usage.Contains("draft", StringComparison.Ordinal)).IsTrue();
    }

    private static ExpectedAnswer Answer(ExpectedDetails details = null) => new ExpectedAnswer(
        Handwritten: false,
        new ExpectedRecipe(new("Cake", true), new("A cake.", true)),
        new ExpectedVariant(new("Classic", true), new("The classic.", true), new(10, true), new(15, true), new(4, true)),
        [new ExpectedIngredient("flour", 1m, "cup")],
        ["Bake it."]) with { Details = details };

    private static ReadResponse ReadWith(DetailsValues details) => new(
        ReadOutcomes.Recipe,
        new RecipeValues(new("Cake", null), new("A cake.", null)),
        new VariantValues(new("Classic", null), new("The classic.", null), new(10, null), new(15, null), new(4, null)),
        [new IngredientValue("flour", 1m, "cup", false, null)],
        [new InstructionValue("Bake it.", null)],
        Match: null,
        PossibleDuplicate: null,
        OtherRecipes: [],
        Recipes: null,
        Details: details);

    private sealed class CountingService : IEvalService
    {
        public int Calls { get; private set; }

        public Task<TimedAttempt> ReadAsync(string model, IReadOnlyList<ReadPhoto> photos, RecipeCatalog catalog, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("The gate must refuse before any read.");
        }

        public Task<TokenCount> CountTokensAsync(string model, IReadOnlyList<ReadPhoto> photos, RecipeCatalog catalog, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("The gate must refuse before any count.");
        }
    }

    private sealed class GateSet : IDisposable
    {
        private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("kcc-readereval-gate-");

        public static RecipeCatalog Catalog() => RecipeCatalog.From([]);

        public static RecipeCatalog CatalogWithCake() => RecipeCatalog.From([new CatalogRecipe("Cake", null, []).Page()]);

        public void Dispose() => root.Delete(recursive: true);

        public IReadOnlyList<EvalCase> Cases(int total, int categories, int nutrition, int converted, bool withDetails = true, bool draft = false, bool judgeable = false)
        {
            var folder = root.CreateSubdirectory(Guid.NewGuid().ToString("N"));
            var cases = new List<EvalCase>();
            for (var index = 0; index < total; index++)
            {
                var directory = folder.CreateSubdirectory($"case-{index}");
                var photos = directory.CreateSubdirectory("photos");
                var photo = Path.Combine(photos.FullName, "1.jpg");
                File.WriteAllBytes(photo, [1]);
                var evalCase = new EvalCase($"case-{index}", directory.FullName, [photo]);
                var nutritionValues = index < nutrition
                    ? new ExpectedNutrition(450, null, null, null, null, null, null, null)
                    : new ExpectedNutrition(null, null, null, null, null, null, null, null);
                var answer = Answer(withDetails
                    ? new ExpectedDetails(
                        index < categories ? "Dinner" : null,
                        [],
                        null,
                        index < nutrition ? (index < converted ? "converted" : "perServing") : null,
                        nutritionValues,
                        Draft: draft && index == 0)
                    : null);
                (judgeable && index == 0 ? answer with { Handwritten = true, Match = "Cake" } : answer).Save(evalCase.ExpectedPath);
                cases.Add(evalCase);
            }

            return cases;
        }
    }

    private static IConfiguration Secrets(string apiKey) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string> { [AnthropicKey.SecretsKey] = apiKey })
        .Build();
}
