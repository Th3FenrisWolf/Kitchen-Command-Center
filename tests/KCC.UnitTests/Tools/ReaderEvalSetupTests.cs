using KCC.ReaderEval;
using KCC.Web.Features.RecipeImport;
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

    private static IConfiguration Secrets(string apiKey) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string> { [AnthropicKey.SecretsKey] = apiKey })
        .Build();
}
