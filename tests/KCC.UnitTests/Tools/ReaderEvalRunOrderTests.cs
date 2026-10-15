using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using KCC.ReaderEval;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace KCC.UnitTests.Tools;

public class ReaderEvalRunOrderTests
{
    private const string Haiku = "claude-haiku-5-5";
    private const string Opus = "claude-opus-5-5";
    private const string Read = "read";
    private const string Count = "count";
    private const long CatalogTokens = 40;

    private static readonly ReaderOutput Pancakes = new(
        Found: true,
        new ReaderRecipe(
            new ReaderText("Pancakes", null),
            new ReaderText("Fluffy weekend pancakes.", new ReaderMark(MarkKinds.Written, "Not in the photos"))),
        new ReaderVariant(
            new ReaderText("Buttermilk", null),
            new ReaderText("Tangy and tall.", null),
            new ReaderCount(10, new ReaderMark(MarkKinds.Estimate, "Not printed")),
            new ReaderCount(15, null),
            new ReaderCount(4, null)),
        [new ReaderIngredient("flour", 1.5m, "cups", false, null), new ReaderIngredient("salt", null, string.Empty, true, null)],
        [new ReaderStep("Whisk the dry ingredients.", null), new ReaderStep("Fry in butter.", null)],
        MatchId: null,
        MatchReason: null,
        OtherRecipes: [],
        SourceLanguage: null);

    [Test]
    public async Task EachModelsTimedReads_ItsColdFirstCallAndThatCaseReadAgainIncluded_ComeBeforeAnyOfItsCountTokensCalls()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("b", 210, 211), set.Case("handwritten-c", 220)];
        var service = new FakeEvalService(set);

        _ = await Runner(service, catalog: Catalog()).CompareAsync(cases, [Haiku, Opus], 2000, CancellationToken.None);

        foreach (var model in new[] { Haiku, Opus })
        {
            var calls = service.Calls.Where(call => call.Model == model).ToList();
            var lastRead = calls.FindLastIndex(call => call.Kind == Read);
            var firstCount = calls.FindIndex(call => call.Kind == Count);
            _ = await Assert.That(calls.Where(call => call.Kind == Read).Select(call => call.Case))
                .IsEquivalentTo(["a", "a", "b", "handwritten-c"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(firstCount).IsGreaterThan(lastRead);
            _ = await Assert.That(calls.Count(call => call.Kind == Count)).IsEqualTo(6);
        }
    }

    [Test]
    public async Task TheFirstCall_IsTheFirstCaseOfUpToThreePhotos()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200, 201, 202, 203), set.Case("b", 210), set.Case("handwritten-c", 220)];
        var service = new FakeEvalService(set);

        _ = await Runner(service, catalog: Catalog()).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        _ = await Assert.That(service.Calls.Where(call => call.Kind == Read).Select(call => call.Case))
            .IsEquivalentTo(["b", "b", "a", "handwritten-c"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task TheFirstCall_IsTimedApart_AndThatCaseReadAgainGivesTheColdPenalty()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("b", 210), set.Case("handwritten-c", 220)];
        var service = new FakeEvalService(set)
        {
            Seconds = readOfModel => readOfModel switch
            {
                0 => 30,
                1 => 12,
                _ => 10,
            },
        };

        var comparison = await Runner(service, catalog: Catalog()).CompareAsync(cases, [Haiku, Opus], 2000, CancellationToken.None);

        foreach (var timing in comparison.Models.Select(model => model.Timing))
        {
            _ = await Assert.That(timing.Cold.Elapsed).IsEqualTo(TimeSpan.FromSeconds(30));
            _ = await Assert.That(timing.Cold.Case).IsEqualTo("a");
            _ = await Assert.That(timing.Again.Elapsed).IsEqualTo(TimeSpan.FromSeconds(12));
            _ = await Assert.That(timing.Again.Case).IsEqualTo("a");
            _ = await Assert.That(timing.Others.Select(read => read.Case))
                .IsEquivalentTo(["b", "handwritten-c"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            _ = await Assert.That(timing.ColdPenalty).IsEqualTo(TimeSpan.FromSeconds(18));
        }

        _ = await Assert.That(comparison.LargestColdPenalty).IsEqualTo(TimeSpan.FromSeconds(18));
    }

    [Test]
    [Arguments("claude-haiku-4-5", 2000, 1568)]
    [Arguments(Opus, 2000, 2000)]
    [Arguments(Opus, 1200, 1200)]
    public async Task EveryPhoto_IsEncodedBeforeItsRead_AtTheLowerOfTheRunsEdgeAndTheModelsImageEdge(
        string model,
        int edge,
        int longEdge)
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 3000), set.Case("handwritten-b", 3010)];
        var service = new FakeEvalService(set);

        var comparison = await Runner(service, catalog: Catalog()).CompareAsync(cases, [model], edge, CancellationToken.None);

        var sent = service.Calls.Where(call => call.Photos.Count > 0).SelectMany(call => call.Photos).ToList();
        _ = await Assert.That(sent).IsNotEmpty();
        _ = await Assert.That(sent.All(photo => photo.MediaType == "image/jpeg")).IsTrue();
        _ = await Assert.That(sent.All(photo => LongEdge(photo) == longEdge)).IsTrue();
        _ = await Assert.That(comparison.Models.Single().Edge).IsEqualTo(longEdge);
    }

    [Test]
    public async Task TheImageTokens_AreCountedPhotoByPhoto_AgainstThePromptAndSchemaWithNoPhoto()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210, 220)];
        var service = new FakeEvalService(set);

        var comparison = await Runner(service, catalog: Catalog()).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        var tokens = comparison.Models.Single().ImageTokens;
        _ = await Assert.That(tokens.PromptTokens).IsEqualTo(3_000L);
        _ = await Assert.That(tokens.PerPhoto).IsEquivalentTo([4_000L, 4_200L, 4_400L], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(tokens.Error).IsNull();
    }

    [Test]
    public async Task EachModel_CountsThePromptWithAndWithoutTheCatalogAfterItsReads_AndTheReportGivesTheCatalogsCharactersPerToken()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210)];
        var catalog = Catalog();
        var service = new FakeEvalService(set);
        var output = new StringWriter();

        _ = await Runner(service, output, catalog: catalog).CompareAsync(cases, [Haiku, Opus], 2000, CancellationToken.None);

        foreach (var model in new[] { Haiku, Opus })
        {
            var calls = service.Calls.Where(call => call.Model == model).ToList();
            var lastRead = calls.FindLastIndex(call => call.Kind == Read);
            var withCatalog = calls.FindIndex(call => call.Kind == Count && call.Catalog == catalog);
            var withoutCatalog = calls.FindIndex(call => call.Kind == Count && call.Catalog is null && call.Photos.Count == 0);
            _ = await Assert.That(calls.Count(call => call.Kind == Count && call.Catalog is not null)).IsEqualTo(1);
            _ = await Assert.That(calls[withCatalog].Photos).IsEmpty();
            _ = await Assert.That(withCatalog).IsGreaterThan(lastRead);
            _ = await Assert.That(withoutCatalog).IsGreaterThan(lastRead);
        }

        var perToken = string.Create(CultureInfo.InvariantCulture, $"{catalog.Characters / (double)CatalogTokens:0.00}");
        _ = await Assert.That(output.ToString()).Contains($"{perToken} characters a token");
    }

    [Test]
    public async Task TheComparison_ScoresEachCaseOnTheReadOfItsFirstCall()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210)];
        var service = new FakeEvalService(set);

        var comparison = await Runner(service, catalog: Catalog()).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        var scores = comparison.Models.Single().Cases;
        _ = await Assert.That(scores.Select(score => score.Case))
            .IsEquivalentTo(["a", "handwritten-b"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(scores.All(score => score.RightLines == 2 && score.StepsInOrder)).IsTrue();
        _ = await Assert.That(comparison.Models.Single().MeanCostMicroUsd)
            .IsEqualTo(ReadCost.MicroUsd(ModelProfiles.Find(Haiku), Usage()));
    }

    [Test]
    public async Task WithACatalog_EveryReadCarriesIt_AndEachCaseIsScoredOnTheRecipeItsReadProposes()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200, match: "Pancakes"), set.Case("handwritten-b", 210)];
        var catalog = Catalog();
        var service = new FakeEvalService(set)
        {
            Answered = _ => new TimedAttempt(
                ReadAttempt.Read(Pancakes with { MatchId = "R1", MatchReason = "Same stack." }, Usage()),
                TimeSpan.FromSeconds(10),
                null),
        };

        var comparison = await Runner(service, catalog: catalog).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        _ = await Assert.That(service.Calls.Where(call => call.Kind == Read).All(call => call.Catalog == catalog)).IsTrue();
        var scores = comparison.Models.Single().Cases;
        _ = await Assert.That(scores[0].Match).IsEqualTo(new MatchScore("Pancakes", "Pancakes"));
        _ = await Assert.That(scores[1].Match).IsEqualTo(new MatchScore(null, "Pancakes"));
    }

    [Test]
    [Arguments("no catalog", "catalog.json is missing, so SC-005")]
    [Arguments("no on-site case", "no case expects a match, so the on-site half of SC-005")]
    [Arguments("no off-site case", "every case expects a match, so the off-site half of SC-005")]
    [Arguments("no handwritten case", "no handwritten case expects an ingredient, so SC-003")]
    [Arguments("no printed case", "no printed case expects an ingredient, so SC-003")]
    [Arguments("no handwritten ingredient", "no handwritten case expects an ingredient, so SC-003")]
    [Arguments("no printed ingredient", "no printed case expects an ingredient, so SC-003")]
    [Arguments("no case of up to 3 photos", "every case has more than 3 photos, so SC-002")]
    public async Task ASetThatCannotJudgeACriterion_StopsTheComparisonBeforeAnyReadOrConfirmation_AndSaysWhy(
        string missing,
        string problem)
    {
        using var set = new TempSet();
        EvalCase[] cases = missing switch
        {
            "no on-site case" => [set.Case("a", 200), set.Case("handwritten-b", 210)],
            "no off-site case" => [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", "Waffles", 210)],
            "no handwritten case" => [set.Case("a", "Pancakes", 200), set.Case("b", 210)],
            "no printed case" => [set.Case("handwritten-a", "Pancakes", 200), set.Case("handwritten-b", 210)],
            "no case of up to 3 photos" =>
                [set.Case("a", "Pancakes", 200, 201, 202, 203), set.Case("handwritten-b", 210, 211, 212, 213)],
            _ => [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210)],
        };
        var emptied = missing switch
        {
            "no handwritten ingredient" => cases[1],
            "no printed ingredient" => cases[0],
            _ => null,
        };
        if (emptied is not null)
        {
            (ExpectedAnswer.Load(emptied.ExpectedPath) with { Ingredients = [] }).Save(emptied.ExpectedPath);
        }

        var service = new FakeEvalService(set);
        var output = new StringWriter();
        var asked = false;
        var runner = Runner(service, output, _ => asked = true, missing == "no catalog" ? null : Catalog());

        var comparison = await runner.CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        _ = await Assert.That(comparison).IsNull();
        _ = await Assert.That(service.Calls).IsEmpty();
        _ = await Assert.That(asked).IsFalse();
        _ = await Assert.That(output.ToString()).Contains(problem);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task AnExpectedMatchThatNamesNoCatalogRecipe_StopsTheComparisonBeforeAnyRead(bool withCatalog)
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200, match: "Crepes"), set.Case("b", 210)];
        var service = new FakeEvalService(set);
        var output = new StringWriter();
        var catalog = withCatalog ? RecipeCatalog.From([Page("Pancakes")]) : null;

        var comparison = await Runner(service, output, catalog: catalog).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        _ = await Assert.That(comparison).IsNull();
        _ = await Assert.That(service.Calls).IsEmpty();
        _ = await Assert.That(output.ToString().Contains("Crepes", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task TheConfirmation_PricesEachReadWithTheCatalogsCharacters()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200, match: "Pancakes"), set.Case("handwritten-b", 210)];
        var catalog = Catalog();
        var questions = new List<string>();
        var runner = Runner(new FakeEvalService(set), confirm: question =>
        {
            questions.Add(question);
            return false;
        }, catalog: catalog);

        _ = await runner.CompareAsync(cases, [Opus], 2000, CancellationToken.None);

        var estimate = 3 * ReadCost.ReservationMicroUsd(ModelProfiles.Find(Opus), 1, catalog.Characters);
        _ = await Assert.That(questions.Single().Contains(Report.Usd(estimate), StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ADraft_WithACatalog_WritesTheProposedRecipeAsTheExpectedMatch()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200, expected: false)];
        var service = new FakeEvalService(set)
        {
            Answered = _ => new TimedAttempt(
                ReadAttempt.Read(Pancakes with { MatchId = "R2", MatchReason = "Same batter." }, Usage()),
                TimeSpan.FromSeconds(10),
                null),
        };

        _ = await Runner(service, catalog: RecipeCatalog.From([Page("Pancakes"), Page("Waffles")]))
            .DraftAsync(cases, Opus, 2000, CancellationToken.None);

        _ = await Assert.That(ExpectedAnswer.Load(cases[0].DraftPath).Match).IsEqualTo("Waffles");
    }

    [Test]
    public async Task ADeclinedConfirmation_MakesNoPaidReadAndNoCount()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210), set.Case("c", 220, expected: false)];
        var service = new FakeEvalService(set);
        var asked = 0;
        var runner = Runner(service, confirm: _ => ++asked < 0, catalog: Catalog());

        var comparison = await runner.CompareAsync([cases[0], cases[1]], [Haiku, Opus], 2000, CancellationToken.None);
        var drafted = await runner.DraftAsync([cases[2]], Opus, 2000, CancellationToken.None);

        _ = await Assert.That(comparison).IsNull();
        _ = await Assert.That(drafted).IsNotEqualTo(0);
        _ = await Assert.That(asked).IsEqualTo(2);
        _ = await Assert.That(service.Calls).IsEmpty();
        _ = await Assert.That(File.Exists(cases[2].DraftPath)).IsFalse();
    }

    [Test]
    public async Task TheConfirmation_StatesTheEstimatedSpend_AsTheSumOfTheReservationsOfTheRunsReads()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", "Pancakes", 200), set.Case("handwritten-b", 210, 220)];
        var catalog = Catalog();
        var questions = new List<string>();
        var runner = Runner(new FakeEvalService(set), confirm: question =>
        {
            questions.Add(question);
            return false;
        }, catalog: catalog);

        _ = await runner.CompareAsync(cases, [Haiku, Opus], 2000, CancellationToken.None);

        var estimate = new[] { Haiku, Opus }.Sum(model =>
            (2 * ReadCost.ReservationMicroUsd(ModelProfiles.Find(model), 1, catalog.Characters))
            + ReadCost.ReservationMicroUsd(ModelProfiles.Find(model), 2, catalog.Characters));
        _ = await Assert.That(questions.Single().Contains(Report.Usd(estimate), StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("y", true)]
    [Arguments("yes", true)]
    [Arguments(" Y ", true)]
    [Arguments("", false)]
    [Arguments("n", false)]
    [Arguments("sure", false)]
    public async Task TheConsoleConfirmation_GoesAheadOnYesOnly(string answer, bool goesAhead)
    {
        var confirmed = Confirmation.Ask(new StringReader(answer + "\n"), TextWriter.Null, "Go ahead?");

        _ = await Assert.That(confirmed).IsEqualTo(goesAhead);
    }

    [Test]
    public async Task TheConsoleConfirmation_DoesNotGoAhead_AtTheEndOfItsInput()
    {
        var confirmed = Confirmation.Ask(new StringReader(string.Empty), TextWriter.Null, "Go ahead?");

        _ = await Assert.That(confirmed).IsFalse();
    }

    [Test]
    public async Task ACheck_MakesOneCountTokensCallPerModel_WithNoPhoto_AndNoRead()
    {
        using var set = new TempSet();
        var service = new FakeEvalService(set);
        var output = new StringWriter();

        var exitCode = await Runner(service, output).CheckAsync([Haiku, Opus], CancellationToken.None);

        _ = await Assert.That(exitCode).IsEqualTo(0);
        _ = await Assert.That(service.Calls.Select(call => (call.Kind, call.Model, call.Photos.Count)))
            .IsEquivalentTo([(Count, Haiku, 0), (Count, Opus, 0)], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(output.ToString().Contains("3,000", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ACheck_ReportsAModelWhoseEndpointRefusedTheRequest()
    {
        using var set = new TempSet();
        var service = new FakeEvalService(set)
        {
            Counted = (model, _) => model == Opus
                ? new TokenCount(null, "HTTP 400 invalid_request_error: output_config.format: bad schema")
                : new TokenCount(3_000, null),
        };
        var output = new StringWriter();

        var exitCode = await Runner(service, output).CheckAsync([Haiku, Opus], CancellationToken.None);

        _ = await Assert.That(exitCode).IsNotEqualTo(0);
        _ = await Assert.That(output.ToString().Contains("bad schema", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ADraft_WritesExpectedDraftJson_ForEachCaseThatHasNoExpectedAnswerYet()
    {
        using var set = new TempSet();
        EvalCase[] cases =
        [
            set.Case("printed-pancakes", 200, expected: false),
            set.Case("handwritten-pancakes", 210, expected: false),
            set.Case("checked", 220),
            set.Case("drafted", 230, expected: false),
        ];
        await File.WriteAllTextAsync(cases[3].DraftPath, "{}");
        var service = new FakeEvalService(set);

        var exitCode = await Runner(service).DraftAsync(cases, Opus, 2000, CancellationToken.None);

        _ = await Assert.That(exitCode).IsEqualTo(0);
        _ = await Assert.That(service.Calls.Select(call => (call.Kind, call.Model, call.Case)))
            .IsEquivalentTo(
                [(Read, Opus, "printed-pancakes"), (Read, Opus, "handwritten-pancakes")],
                TUnit.Assertions.Enums.CollectionOrdering.Matching);
        var printed = ExpectedAnswer.Load(cases[0].DraftPath);
        var handwritten = ExpectedAnswer.Load(cases[1].DraftPath);
        _ = await Assert.That(printed.Handwritten).IsFalse();
        _ = await Assert.That(handwritten.Handwritten).IsTrue();
        _ = await Assert.That(printed.Recipe.Name).IsEqualTo(new ExpectedValue<string>("Pancakes", true));
        _ = await Assert.That(printed.Recipe.Description).IsEqualTo(new ExpectedValue<string>("Fluffy weekend pancakes.", false));
        _ = await Assert.That(printed.Variant.PrepMinutes).IsEqualTo(new ExpectedValue<int?>(10, false));
        _ = await Assert.That(printed.Variant.CookMinutes).IsEqualTo(new ExpectedValue<int?>(15, true));
        _ = await Assert.That(printed.Ingredients).IsEquivalentTo(
            [new ExpectedIngredient("flour", 1.5m, "cups"), new ExpectedIngredient("salt", null, string.Empty)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(printed.Steps).IsEquivalentTo(
            ["Whisk the dry ingredients.", "Fry in butter."],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        _ = await Assert.That(await File.ReadAllTextAsync(cases[3].DraftPath)).IsEqualTo("{}");
    }

    [Test]
    public async Task ADraft_Stops_WhenItsFirstReadIsAServiceError()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200, expected: false), set.Case("b", 210, expected: false)];
        var service = new FakeEvalService(set)
        {
            Answered = _ => new TimedAttempt(ReadAttempt.ServiceError(), TimeSpan.FromSeconds(1), "HTTP 400 invalid_request_error: schema"),
        };
        var output = new StringWriter();

        var exitCode = await Runner(service, output).DraftAsync(cases, Opus, 2000, CancellationToken.None);

        _ = await Assert.That(exitCode).IsNotEqualTo(0);
        _ = await Assert.That(service.Calls.Count).IsEqualTo(1);
        _ = await Assert.That(File.Exists(cases[0].DraftPath)).IsFalse();
        _ = await Assert.That(output.ToString().Contains("HTTP 400 invalid_request_error: schema", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task AComparison_Stops_WhenACaseHasNoExpectedAnswer()
    {
        using var set = new TempSet();
        EvalCase[] cases = [set.Case("a", 200), set.Case("b", 210, expected: false)];
        var service = new FakeEvalService(set);
        var output = new StringWriter();

        var comparison = await Runner(service, output).CompareAsync(cases, [Haiku], 2000, CancellationToken.None);

        _ = await Assert.That(comparison).IsNull();
        _ = await Assert.That(service.Calls).IsEmpty();
        _ = await Assert.That(output.ToString().Contains("expected.json", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments(Opus)]
    [Arguments("claude-haiku-4-5")]
    public async Task ACountTokensCall_CarriesTheReadersPromptOutputSchemaAndThinking_ApartFromMaxTokens(string model)
    {
        var handler = new FakeAnthropic();
        var service = Service(handler);
        ReadPhoto[] photos = [new ReadPhoto("image/jpeg", Jpeg(120, 90))];

        _ = await service.ReadAsync(model, photos, null, CancellationToken.None);
        var counted = await service.CountTokensAsync(model, photos, null, CancellationToken.None);

        var read = handler.Sent.Single(sent => sent.Path == "/v1/messages").Body;
        var count = handler.Sent.Single(sent => sent.Path == "/v1/messages/count_tokens").Body;
        _ = await Assert.That(counted).IsEqualTo(new TokenCount(1_234, null));
        _ = await Assert.That(read.Remove("max_tokens")).IsTrue();
        _ = await Assert.That(JsonNode.DeepEquals(read, count)).IsTrue();
        _ = await Assert.That(JsonNode.DeepEquals(count["output_config"]?["format"]?["schema"], EmbeddedSchema())).IsTrue();
    }

    [Test]
    public async Task ACountTokensCallWithACatalog_CarriesTheSameMessageAsAReadWithIt_ApartFromMaxTokens()
    {
        var handler = new FakeAnthropic();
        var service = Service(handler);
        var catalog = Catalog();

        _ = await service.ReadAsync(Opus, [], catalog, CancellationToken.None);
        _ = await service.CountTokensAsync(Opus, [], catalog, CancellationToken.None);

        var read = handler.Sent.Single(sent => sent.Path == "/v1/messages").Body;
        var count = handler.Sent.Single(sent => sent.Path == "/v1/messages/count_tokens").Body;
        _ = await Assert.That(read.Remove("max_tokens")).IsTrue();
        _ = await Assert.That(JsonNode.DeepEquals(read, count)).IsTrue();
        _ = await Assert.That(count.ToJsonString()).Contains("Waffles");
    }

    [Test]
    public async Task ACountTokensCallWithNoPhoto_SendsThePromptAndTheSchemaAlone()
    {
        var handler = new FakeAnthropic();

        _ = await Service(handler).CountTokensAsync(Haiku, [], null, CancellationToken.None);

        var count = handler.Sent.Single().Body;
        var content = count["messages"]!.AsArray().Single()!["content"]!.AsArray();
        _ = await Assert.That(content.Count).IsEqualTo(1);
        _ = await Assert.That(content[0]!["type"]!.GetValue<string>()).IsEqualTo("text");
        _ = await Assert.That(count["system"]!.GetValue<string>()).IsEqualTo(ReaderPrompt.System);
        _ = await Assert.That(JsonNode.DeepEquals(count["output_config"]?["format"]?["schema"], EmbeddedSchema())).IsTrue();
    }

    [Test]
    public async Task AReadThatTheServiceRefuses_IsDescribedByItsStatusAndMessage()
    {
        var handler = new FakeAnthropic
        {
            Status = HttpStatusCode.BadRequest,
            ErrorMessage = "output_config.format.schema: unsupported keyword",
        };

        var timed = await Service(handler).ReadAsync(Opus, [new ReadPhoto("image/jpeg", Jpeg(120, 90))], null, CancellationToken.None);

        _ = await Assert.That(timed.Attempt.Failure).IsEqualTo(ReadFailure.ServiceError);
        _ = await Assert.That(timed.ServiceError.Contains("400", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(timed.ServiceError.Contains("unsupported keyword", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ACountThatTheServiceRefuses_IsDescribedByItsStatusAndMessage()
    {
        var handler = new FakeAnthropic
        {
            Status = HttpStatusCode.BadRequest,
            ErrorMessage = "output_config.format.schema: unsupported keyword",
        };

        var counted = await Service(handler).CountTokensAsync(Opus, [], null, CancellationToken.None);

        _ = await Assert.That(counted.InputTokens).IsNull();
        _ = await Assert.That(counted.Error.Contains("400", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(counted.Error.Contains("unsupported keyword", StringComparison.Ordinal)).IsTrue();
    }

    private static EvalRunner Runner(
        IEvalService service,
        TextWriter output = null,
        Func<string, bool> confirm = null,
        RecipeCatalog catalog = null) =>
        new(service, output ?? TextWriter.Null, confirm ?? (_ => true), catalog);

    private static RecipeCatalog Catalog() => RecipeCatalog.From([Page("Pancakes"), Page("Waffles")]);

    private static RecipePageData Page(string name) =>
        new(new RecipeRecord(Guid.NewGuid(), name, null, $"{name}, for one test.", null, null, null, null, default), [], null);

    private static AnthropicEvalService Service(HttpMessageHandler handler)
    {
        var anthropic = new AnthropicOptions { ApiKey = "test-key" };
        var recorder = new ServiceErrorRecorder { InnerHandler = handler };
        var client = new AnthropicClient(AnthropicClientOptionsFactory.Create(anthropic) with { HttpClient = new HttpClient(recorder) });
        return new AnthropicEvalService(client, anthropic, recorder, TimeProvider.System);
    }

    private static Usage Usage() => new()
    {
        InputTokens = 9_000,
        OutputTokens = 1_500,
        CacheCreationInputTokens = null,
        CacheReadInputTokens = null,
        CacheCreation = null,
        InferenceGeo = null,
        OutputTokensDetails = null,
        ServerToolUse = null,
        ServiceTier = null,
    };

    private static int LongEdge(ReadPhoto photo)
    {
        var info = Image.Identify(photo.Data);
        return Math.Max(info.Width, info.Height);
    }

    private static byte[] Jpeg(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(240, 230, 200));
        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder { Quality = 90 });
        return stream.ToArray();
    }

    private static JsonNode EmbeddedSchema()
    {
        using var stream = typeof(AnthropicRecipePhotoReader).Assembly
            .GetManifestResourceStream("KCC.Web.Features.RecipeImport.ReaderOutputSchema.json");
        return JsonNode.Parse(stream);
    }

    private sealed record Call(string Kind, string Model, string Case, IReadOnlyList<ReadPhoto> Photos, RecipeCatalog Catalog = null);

    private sealed class FakeEvalService(TempSet set) : IEvalService
    {
        private readonly Dictionary<string, int> readsByModel = [];

        public List<Call> Calls { get; } = [];

        public Func<int, double> Seconds { get; init; } = _ => 10;

        public Func<int, TimedAttempt> Answered { get; init; }

        public Func<string, IReadOnlyList<ReadPhoto>, TokenCount> Counted { get; init; } =
            (_, photos) => new TokenCount(3_000 + photos.Sum(photo => 20L * Image.Identify(photo.Data).Width), null);

        public Task<TokenCount> CountTokensAsync(
            string model,
            IReadOnlyList<ReadPhoto> photos,
            RecipeCatalog catalog,
            CancellationToken cancellationToken)
        {
            Calls.Add(new Call(Count, model, set.CaseOf(photos), photos, catalog));
            var counted = Counted(model, photos);
            return Task.FromResult(catalog is null || counted.InputTokens is not { } tokens
                ? counted
                : counted with { InputTokens = tokens + CatalogTokens });
        }

        public Task<TimedAttempt> ReadAsync(
            string model,
            IReadOnlyList<ReadPhoto> photos,
            RecipeCatalog catalog,
            CancellationToken cancellationToken)
        {
            Calls.Add(new Call(Read, model, set.CaseOf(photos), photos, catalog));
            var readOfModel = readsByModel[model] = readsByModel.GetValueOrDefault(model) + 1;
            return Task.FromResult(Answered?.Invoke(readOfModel - 1) ?? new TimedAttempt(
                ReadAttempt.Read(Pancakes, Usage()),
                TimeSpan.FromSeconds(Seconds(readOfModel - 1)),
                null));
        }
    }

    private sealed class TempSet : IDisposable
    {
        private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("kcc-readereval-");
        private readonly Dictionary<int, string> caseByWidth = [];

        public EvalCase Case(string name, params int[] widths) => Case(name, true, null, widths);

        public EvalCase Case(string name, int width, bool expected) => Case(name, expected, null, [width]);

        public EvalCase Case(string name, int width, string match) => Case(name, true, match, [width]);

        public EvalCase Case(string name, string match, params int[] widths) => Case(name, true, match, widths);

        public string CaseOf(IReadOnlyList<ReadPhoto> photos) =>
            photos.Count == 0 ? null : caseByWidth.GetValueOrDefault(Image.Identify(photos[0].Data).Width);

        public void Dispose() => root.Delete(recursive: true);

        private EvalCase Case(string name, bool expected, string match, int[] widths)
        {
            var directory = root.CreateSubdirectory(name);
            var photos = directory.CreateSubdirectory("photos");
            var paths = new List<string>();
            for (var index = 0; index < widths.Length; index++)
            {
                var path = Path.Combine(photos.FullName, $"{index + 1}.jpg");
                File.WriteAllBytes(path, Jpeg(widths[index], widths[index] >= 3000 ? 2000 : 100));
                paths.Add(path);
                caseByWidth[widths[index]] = name;
            }

            var evalCase = new EvalCase(name, directory.FullName, paths);
            if (expected)
            {
                new ExpectedAnswer(
                    Handwritten: name.StartsWith("handwritten", StringComparison.Ordinal),
                    new ExpectedRecipe(new("Pancakes", true), new("Fluffy weekend pancakes.", false)),
                    new ExpectedVariant(new("Buttermilk", true), new("Tangy and tall.", true), new(10, false), new(15, true), new(4, true)),
                    [new ExpectedIngredient("flour", 1.5m, "cups"), new ExpectedIngredient("salt", null, string.Empty)],
                    ["Whisk the dry ingredients.", "Fry in butter."],
                    match).Save(evalCase.ExpectedPath);
            }

            return evalCase;
        }
    }

    private sealed class FakeAnthropic : HttpMessageHandler
    {
        private readonly ConcurrentQueue<(string Path, JsonObject Body)> sent = new();

        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        public string ErrorMessage { get; init; }

        public IReadOnlyList<(string Path, JsonObject Body)> Sent => [.. sent];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken))!.AsObject();
            sent.Enqueue((request.RequestUri!.AbsolutePath, body));

            object answer = Status != HttpStatusCode.OK
                ? new { type = "error", error = new { type = "invalid_request_error", message = ErrorMessage } }
                : request.RequestUri.AbsolutePath.EndsWith("/count_tokens", StringComparison.Ordinal)
                    ? new { input_tokens = 1_234 }
                    : Message();
            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(JsonSerializer.Serialize(answer), Encoding.UTF8, "application/json"),
            };
        }

        private static object Message() => new
        {
            id = "msg_test",
            type = "message",
            role = "assistant",
            model = Opus,
            content = new object[]
            {
                new { type = "text", text = JsonSerializer.Serialize(Pancakes, new JsonSerializerOptions(JsonSerializerDefaults.Web)), citations = (object)null },
            },
            stop_reason = "end_turn",
            stop_sequence = (string)null,
            usage = new { input_tokens = 9_000, output_tokens = 1_500 },
        };
    }
}
