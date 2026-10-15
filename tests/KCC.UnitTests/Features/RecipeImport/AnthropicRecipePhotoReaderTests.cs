using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using KCC.UnitTests.Features.Providers;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.RecipeImport;

public class AnthropicRecipePhotoReaderTests
{
    private const string ApiKey = "test-key";
    private const string ModelIcon = "fa-duotone fa-egg-fried";

    private static readonly byte[] JpegPhoto =
        [0xFF, 0xD8, 0xFF, 0xE0, .. Enumerable.Range(0, 240).Select(index => (byte)((index * 37) + 11))];

    private static readonly byte[] PngPhoto =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFB, 0xEF, 0xBE, 0xFF, 0xFF, 0xFF];

    private static readonly string[] LoggedValues =
    [
        "Outcome",
        "Model",
        "InputTokens",
        "OutputTokens",
        "CacheCreationInputTokens",
        "CacheReadInputTokens",
        "CostMicroUsd",
        "{OriginalFormat}",
    ];

    private const string ReaderOutputJson =
        """
        {
          "found": true,
          "recipe": {
            "name": { "value": "Pancakes", "mark": null },
            "description": { "value": "Fluffy weekend pancakes.", "mark": { "kind": "written", "reason": "Not in the photos" } }
          },
          "variant": {
            "name": { "value": "Buttermilk", "mark": null },
            "description": { "value": "Tangy and tall.", "mark": null },
            "prepMinutes": { "value": 10, "mark": { "kind": "estimate", "reason": "Not printed" } },
            "cookMinutes": { "value": 15, "mark": null },
            "servings": { "value": 4, "mark": null }
          },
          "ingredients": [
            { "name": "flour", "quantity": 1.5, "unit": "cups", "toTaste": false, "mark": null },
            { "name": "garlic", "quantity": 2, "unit": "cloves", "toTaste": false,
              "mark": { "kind": "unclear", "reason": "Printed as 2 to 3" } },
            { "name": "salt", "quantity": null, "unit": "", "toTaste": true, "mark": null }
          ],
          "steps": [
            { "text": "Whisk the dry ingredients.", "mark": null },
            { "text": "Fry in butter.", "mark": null }
          ],
          "matchId": null,
          "matchReason": null,
          "otherRecipes": ["Maple syrup"],
          "sourceLanguage": null
        }
        """;

    [Test]
    [Arguments("claude-haiku-4-5")]
    [Arguments("claude-haiku-5-5")]
    [Arguments("claude-sonnet-5-5")]
    [Arguments("claude-opus-5-5")]
    public async Task IsConfigured_WithAnApiKeyAndAModelThatHasAProfile_IsTrue(string model)
    {
        var reader = Reader(FakeAnthropic.Answering(RecipeAnswer), model: model);

        _ = await Assert.That(reader.IsConfigured).IsTrue();
    }

    [Test]
    [Arguments((string)null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task IsConfigured_WithoutAnApiKey_IsFalse(string apiKey)
    {
        var reader = Reader(FakeAnthropic.Answering(RecipeAnswer), apiKey: apiKey);

        _ = await Assert.That(reader.IsConfigured).IsFalse();
    }

    [Test]
    public async Task IsConfigured_ForAModelWithoutAProfile_IsFalse_AndLogsAnErrorThatNamesTheModel()
    {
        var logger = new RecordingLogger();
        var reader = Reader(FakeAnthropic.Answering(RecipeAnswer), model: "claude-opus-4-1", logger: logger);

        var configured = reader.IsConfigured;

        _ = await Assert.That(configured).IsFalse();
        _ = await Assert.That(logger.Entries.Any(entry =>
            entry.Level == LogLevel.Error && entry.Message.Contains("claude-opus-4-1", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    [Arguments(ImportFlows.Recipe)]
    [Arguments(ImportFlows.Variant)]
    public async Task ARead_SendsEachPhotoAsABase64ImageBlock_ThenTheTextBlock(string flow)
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);
        var request = Request(flow, new ReadPhoto("image/jpeg", JpegPhoto), new ReadPhoto("image/png", PngPhoto));

        _ = await Reader(handler).ReadAsync(request, CancellationToken.None);

        var sent = SentParameters(handler.Bodies.Single());
        _ = await Assert.That(sent.Messages.Count).IsEqualTo(1);
        _ = await Assert.That(sent.Messages[0].Content.TryPickContentBlockParams(out var blocks)).IsTrue();
        _ = await Assert.That(blocks.Count).IsEqualTo(3);
        _ = await Assert.That(Base64Source(blocks[0])).IsEqualTo(("image/jpeg", Convert.ToBase64String(JpegPhoto)));
        _ = await Assert.That(Base64Source(blocks[1])).IsEqualTo(("image/png", Convert.ToBase64String(PngPhoto)));
        _ = await Assert.That(blocks[2].TryPickText(out var text)).IsTrue();
        _ = await Assert.That(text.Text).IsEqualTo(ReaderPrompt.Instruction(request));
        _ = await Assert.That(sent.System.TryPickString(out var system)).IsTrue();
        _ = await Assert.That(system).IsEqualTo(ReaderPrompt.System);
    }

    [Test]
    public async Task ARecipeFlowRead_CarriesTheCatalogThatItsRequestHolds_InItsTextBlock()
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);
        var catalog = RecipeCatalog.From([Page("Weeknight Tacos", "Taco night.", "Slow Beef Barbacoa"), Page("Golden Milk", "Warm.")]);
        var request = new ReadRequest(ImportFlows.Recipe, null, [new ReadPhoto("image/jpeg", JpegPhoto)], catalog);

        _ = await Reader(handler).ReadAsync(request, CancellationToken.None);

        var text = TextBlock(SentParameters(handler.Bodies.Single()));
        _ = await Assert.That(text).IsEqualTo(ReaderPrompt.Instruction(request));
        _ = await Assert.That(text.Contains(catalog.Text, StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(text.Contains("Slow Beef Barbacoa", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task AVariantFlowRead_WhoseRequestHoldsNoCatalog_CarriesNone()
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);
        var request = new ReadRequest(ImportFlows.Variant, Guid.NewGuid(), [new ReadPhoto("image/jpeg", JpegPhoto)], null);

        _ = await Reader(handler).ReadAsync(request, CancellationToken.None);

        var text = TextBlock(SentParameters(handler.Bodies.Single()));
        _ = await Assert.That(text).IsEqualTo(ReaderPrompt.Instruction(request));
        _ = await Assert.That(text.Contains("\"id\":", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(text.Contains("catalog", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task ARead_AsksForTheEmbeddedSchema()
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);

        _ = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        var format = SentParameters(handler.Bodies.Single()).OutputConfig.Format;
        _ = await Assert.That(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(format.Schema), EmbeddedSchema())).IsTrue();
    }

    [Test]
    public async Task TheEmbeddedSchema_IsTheContractsReaderOutputSchema()
    {
        var contractPath = Path.Combine(
            RepoPaths.Root,
            "specs",
            "001-photo-recipe-import",
            "contracts",
            "reader-output.schema.json");
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(contractPath));

        _ = await Assert.That(JsonElement.DeepEquals(contract.RootElement, EmbeddedSchema())).IsTrue();
    }

    [Test]
    [Arguments("claude-haiku-4-5")]
    [Arguments("claude-haiku-5-5")]
    [Arguments("claude-sonnet-5-5")]
    [Arguments("claude-opus-5-5")]
    public async Task ARead_SendsTheConfiguredModel_With12000MaxTokens(string model)
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);

        _ = await Reader(handler, model: model).ReadAsync(Request(), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Bodies.Single());
        _ = await Assert.That(body.RootElement.GetProperty("model").GetString()).IsEqualTo(model);
        _ = await Assert.That(body.RootElement.GetProperty("max_tokens").GetInt64()).IsEqualTo(12_000L);
    }

    [Test]
    [Arguments("claude-haiku-5-5", "low")]
    [Arguments("claude-sonnet-5-5", "low")]
    [Arguments("claude-opus-5-5", "low")]
    [Arguments("claude-opus-5-5", "medium")]
    public async Task AModelThatThinksAdaptively_GetsAdaptiveThinkingAndTheConfiguredEffort(string model, string effort)
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);

        _ = await Reader(handler, model: model, effort: effort).ReadAsync(Request(), CancellationToken.None);

        var sent = SentParameters(handler.Bodies.Single());
        _ = await Assert.That(sent.Thinking is { } thinking && thinking.TryPickAdaptive(out _)).IsTrue();
        _ = await Assert.That(sent.OutputConfig.Effort?.Raw()).IsEqualTo(effort);
    }

    [Test]
    public async Task HaikuFourFive_GetsNeitherThinkingNorEffort()
    {
        var handler = FakeAnthropic.Answering(RecipeAnswer);

        _ = await Reader(handler, model: "claude-haiku-4-5").ReadAsync(Request(), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Bodies.Single());
        var outputConfig = body.RootElement.GetProperty("output_config");
        _ = await Assert.That(body.RootElement.TryGetProperty("thinking", out _)).IsFalse();
        _ = await Assert.That(outputConfig.TryGetProperty("effort", out _)).IsFalse();
        _ = await Assert.That(outputConfig.TryGetProperty("format", out _)).IsTrue();
    }

    [Test]
    public async Task AnAnswerWithTheReaderOutput_IsARead_ParsedFromItsTextBlock()
    {
        var attempt = await Reader(FakeAnthropic.Answering(RecipeAnswer)).ReadAsync(Request(), CancellationToken.None);

        var output = attempt.Output;
        _ = await Assert.That(attempt.Failure).IsNull();
        _ = await Assert.That(attempt.ServiceAnswered).IsTrue();
        _ = await Assert.That(output.Found).IsTrue();
        _ = await Assert.That(output.Recipe.Name).IsEqualTo(new ReaderText("Pancakes", null));
        _ = await Assert.That(output.Recipe.Description)
            .IsEqualTo(new ReaderText("Fluffy weekend pancakes.", new ReaderMark(MarkKinds.Written, "Not in the photos")));
        _ = await Assert.That(output.Variant.Name).IsEqualTo(new ReaderText("Buttermilk", null));
        _ = await Assert.That(output.Variant.PrepMinutes)
            .IsEqualTo(new ReaderCount(10m, new ReaderMark(MarkKinds.Estimate, "Not printed")));
        _ = await Assert.That(output.Variant.CookMinutes).IsEqualTo(new ReaderCount(15m, null));
        _ = await Assert.That(output.Variant.Servings).IsEqualTo(new ReaderCount(4m, null));
        _ = await Assert.That(output.Ingredients).IsEquivalentTo(
            [
                new ReaderIngredient("flour", 1.5m, "cups", false, null),
                new ReaderIngredient("garlic", 2m, "cloves", false, new ReaderMark(MarkKinds.Unclear, "Printed as 2 to 3")),
                new ReaderIngredient("salt", null, string.Empty, true, null),
            ],
            CollectionOrdering.Matching);
        _ = await Assert.That(output.Steps).IsEquivalentTo(
            [new ReaderStep("Whisk the dry ingredients.", null), new ReaderStep("Fry in butter.", null)],
            CollectionOrdering.Matching);
        _ = await Assert.That(output.MatchId).IsNull();
        _ = await Assert.That(output.OtherRecipes).IsEquivalentTo(["Maple syrup"], CollectionOrdering.Matching);
        _ = await Assert.That(output.SourceLanguage).IsNull();
        _ = await Assert.That(TokensOf(attempt.Usage)).IsEqualTo((10_000L, 2_000L, 800L, 300L));
    }

    [Test]
    [Arguments("refusal", ReadFailure.Refused)]
    [Arguments("max_tokens", ReadFailure.CutOff)]
    public async Task AStopReasonThatEndsTheReadEarly_GivesItsOwnFailure_WithTheUsage(string stopReason, ReadFailure failure)
    {
        var handler = FakeAnthropic.Answering(() => Answer(stopReason, ReaderOutputJson));

        var attempt = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Failure).IsEqualTo(failure);
        _ = await Assert.That(attempt.ServiceAnswered).IsTrue();
        _ = await Assert.That(attempt.Output).IsNull();
        _ = await Assert.That(TokensOf(attempt.Usage)).IsEqualTo((10_000L, 2_000L, 800L, 300L));
    }

    [Test]
    [Arguments("I cannot read this recipe.")]
    [Arguments("{\"found\": true, \"recipe\": ")]
    [Arguments("{\"found\": true}")]
    [Arguments("null")]
    [Arguments("[]")]
    public async Task TextThatIsNotTheReaderOutput_IsUnusable_WithTheUsage(string text)
    {
        var handler = FakeAnthropic.Answering(() => Answer("end_turn", text));

        var attempt = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.Unusable);
        _ = await Assert.That(attempt.ServiceAnswered).IsTrue();
        _ = await Assert.That(attempt.Output).IsNull();
        _ = await Assert.That(TokensOf(attempt.Usage)).IsEqualTo((10_000L, 2_000L, 800L, 300L));
    }

    [Test]
    public async Task AnAnswerWithoutATextBlock_IsUnusable_WithTheUsage()
    {
        var handler = FakeAnthropic.Answering(() => Answer("end_turn", text: null));

        var attempt = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.Unusable);
        _ = await Assert.That(TokensOf(attempt.Usage)).IsEqualTo((10_000L, 2_000L, 800L, 300L));
    }

    [Test]
    [Arguments(400)]
    [Arguments(429)]
    [Arguments(500)]
    [Arguments(529)]
    public async Task AnErrorStatus_IsAServiceErrorThatTheServiceAnswered_WithoutUsage_AfterOneRequest(int status)
    {
        var handler = FakeAnthropic.Answering(() => ErrorAnswer((HttpStatusCode)status, "overloaded_error"));

        var attempt = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.ServiceError);
        _ = await Assert.That(attempt.ServiceAnswered).IsTrue();
        _ = await Assert.That(attempt.Usage).IsNull();
        _ = await Assert.That(handler.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task AConnectionFailure_IsAFailureWithNoAnswer_AfterOneRequest()
    {
        var handler = FakeAnthropic.Failing();

        var attempt = await Reader(handler).ReadAsync(Request(), CancellationToken.None);

        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.NoAnswer);
        _ = await Assert.That(attempt.ServiceAnswered).IsFalse();
        _ = await Assert.That(attempt.Usage).IsNull();
        _ = await Assert.That(handler.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task TheUsage_GivesTheReadsCost_AndTheLogCarriesIt()
    {
        var logger = new RecordingLogger();
        var handler = FakeAnthropic.Answering(RecipeAnswer);

        var attempt = await Reader(handler, logger: logger).ReadAsync(Request(), CancellationToken.None);

        var cost = ReadCost.MicroUsd(ModelProfiles.Find("claude-opus-5-5"), attempt.Usage);
        var logged = logger.Entries.Single();
        _ = await Assert.That(cost).IsEqualTo(84_060L);
        _ = await Assert.That(logged.Value("CostMicroUsd")).IsEqualTo(84_060L);
        _ = await Assert.That(logged.Value("InputTokens")).IsEqualTo(10_000L);
        _ = await Assert.That(logged.Value("OutputTokens")).IsEqualTo(2_000L);
        _ = await Assert.That(logged.Value("Model")).IsEqualTo("claude-opus-5-5");
    }

    [Test]
    public async Task TheReadDeadline_GivesATimedOutFailureWithoutUsage_AndNeverCancelsTheCallersToken()
    {
        var clock = new ManualClock();
        var handler = FakeAnthropic.Holding();
        using var caller = new CancellationTokenSource();

        var read = Reader(handler, readTimeoutSeconds: 85, clock: clock).ReadAsync(Request(), caller.Token);
        var sent = await handler.Received.WaitAsync(TimeSpan.FromSeconds(10));
        clock.Advance(TimeSpan.FromSeconds(84));
        var cancelledBeforeTheDeadline = sent.IsCancellationRequested;
        clock.Advance(TimeSpan.FromSeconds(1));
        var attempt = await read.WaitAsync(TimeSpan.FromSeconds(10));

        _ = await Assert.That(cancelledBeforeTheDeadline).IsFalse();
        _ = await Assert.That(sent.IsCancellationRequested).IsTrue();
        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.TimedOut);
        _ = await Assert.That(attempt.ServiceAnswered).IsFalse();
        _ = await Assert.That(attempt.Usage).IsNull();
        _ = await Assert.That(caller.IsCancellationRequested).IsFalse();
    }

    [Test]
    public async Task ABodyThatStalls_EndsAtTheReadDeadline_AsATimedOutFailure_AndIsDisposed()
    {
        var clock = new ManualClock();
        var handler = new StallingBodyHandler();
        using var caller = new CancellationTokenSource();

        var read = Reader(handler, readTimeoutSeconds: 85, clock: clock).ReadAsync(Request(), caller.Token);
        await handler.BodyStalled.WaitAsync(TimeSpan.FromSeconds(10));
        clock.Advance(TimeSpan.FromSeconds(85));
        var attempt = await read.WaitAsync(TimeSpan.FromSeconds(10));

        _ = await Assert.That(attempt.Failure).IsEqualTo(ReadFailure.TimedOut);
        _ = await Assert.That(attempt.ServiceAnswered).IsFalse();
        _ = await Assert.That(attempt.Usage).IsNull();
        _ = await Assert.That(caller.IsCancellationRequested).IsFalse();
        _ = await Assert.That(handler.BodyDisposed).IsTrue();
    }

    [Test]
    public async Task ACancelledCaller_CancelsTheRead()
    {
        var handler = FakeAnthropic.Holding();
        using var caller = new CancellationTokenSource();

        var read = Reader(handler, clock: new ManualClock()).ReadAsync(Request(), caller.Token);
        _ = await handler.Received.WaitAsync(TimeSpan.FromSeconds(10));
        await caller.CancelAsync();

        _ = await Assert.That(async () => await read.WaitAsync(TimeSpan.FromSeconds(10))).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task TwoReadsAndThenAnIconCall_OnOneSharedClient_AllReachTheService()
    {
        var handler = FakeAnthropic.Answering(body => body.Contains("select_icon", StringComparison.Ordinal) ? IconAnswer() : RecipeAnswer());
        var client = SharedClient(handler);
        var reader = Reader(client);
        var icons = new RecipeIconProvider(client, new AnthropicOptions { ApiKey = ApiKey }, NullLogger<RecipeIconProvider>.Instance);

        var first = await reader.ReadAsync(Request(), CancellationToken.None);
        var second = await reader.ReadAsync(Request(), CancellationToken.None);
        var icon = await icons.PickAsync("Pancakes", "Fluffy weekend pancakes.", ["flour"], CancellationToken.None);

        _ = await Assert.That(first.Failure).IsNull();
        _ = await Assert.That(second.Failure).IsNull();
        _ = await Assert.That(icon).IsEqualTo(ModelIcon);
        _ = await Assert.That(handler.Calls).IsEqualTo(3);
    }

    [Test]
    public async Task NoLogEntry_CarriesImageData_OrAnythingButTheOutcomeModelTokensAndCost()
    {
        var imageData = Convert.ToBase64String(JpegPhoto);
        var logger = new RecordingLogger();
        var answers = new Queue<Func<HttpResponseMessage>>(
        [
            RecipeAnswer,
            () => Answer("end_turn", "Unreadable: " + imageData),
            () => ErrorAnswer(HttpStatusCode.BadRequest, "Could not process image " + imageData),
        ]);
        var reader = Reader(
            FakeAnthropic.Answering(_ => answers.Count > 0 ? answers.Dequeue()() : throw new HttpRequestException(imageData)),
            logger: logger);

        for (var read = 0; read < 4; read++)
        {
            _ = await reader.ReadAsync(Request(), CancellationToken.None);
        }

        _ = await Assert.That(logger.Entries.Count).IsEqualTo(4);
        foreach (var entry in logger.Entries)
        {
            _ = await Assert.That(entry.Message.Contains(imageData, StringComparison.Ordinal)).IsFalse();
            _ = await Assert.That(entry.Message.Contains(imageData[100..132], StringComparison.Ordinal)).IsFalse();
            _ = await Assert.That(entry.Values.All(value => LoggedValues.Contains(value.Key))).IsTrue();
            _ = await Assert.That(entry.Exception).IsNull();
        }
    }

    private static AnthropicRecipePhotoReader Reader(
        HttpMessageHandler handler,
        string model = "claude-opus-5-5",
        string effort = "low",
        string apiKey = ApiKey,
        int readTimeoutSeconds = 85,
        TimeProvider clock = null,
        ILogger<AnthropicRecipePhotoReader> logger = null) =>
        Reader(SharedClient(handler), model, effort, apiKey, readTimeoutSeconds, clock, logger);

    private static AnthropicRecipePhotoReader Reader(
        AnthropicClient client,
        string model = "claude-opus-5-5",
        string effort = "low",
        string apiKey = ApiKey,
        int readTimeoutSeconds = 85,
        TimeProvider clock = null,
        ILogger<AnthropicRecipePhotoReader> logger = null)
    {
        var options = new RecipeImportOptions { Model = model, Effort = effort, ReadTimeoutSeconds = readTimeoutSeconds };
        return new AnthropicRecipePhotoReader(
            client,
            new AnthropicOptions { ApiKey = apiKey },
            Mock.Of<IOptionsMonitor<RecipeImportOptions>>(monitor => monitor.CurrentValue == options),
            clock ?? TimeProvider.System,
            logger ?? NullLogger<AnthropicRecipePhotoReader>.Instance);
    }

    private static AnthropicClient SharedClient(HttpMessageHandler handler) =>
        new(AnthropicClientOptionsFactory.Create(new AnthropicOptions { ApiKey = ApiKey }) with
        {
            HttpClient = new HttpClient(handler),
        });

    private static ReadRequest Request(string flow = ImportFlows.Recipe, params ReadPhoto[] photos) =>
        new(flow, flow == ImportFlows.Variant ? Guid.NewGuid() : null, photos.Length > 0 ? photos : [new ReadPhoto("image/jpeg", JpegPhoto)], null);

    private static string TextBlock(MessageCreateParams sent) =>
        sent.Messages[0].Content.TryPickContentBlockParams(out var blocks) && blocks[^1].TryPickText(out var text) ? text.Text : null;

    private static RecipePageData Page(string name, string description, params string[] variantNames) => new(
        new RecipeRecord(Guid.NewGuid(), name, null, description, null, null, null, null, default),
        [
            .. variantNames.Select(variantName => new VariantRecord(
                Guid.NewGuid(),
                variantName,
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
                default)),
        ],
        null);

    private static MessageCreateParams SentParameters(string body) =>
        MessageCreateParams.FromRawUnchecked(
            new Dictionary<string, JsonElement>(),
            new Dictionary<string, JsonElement>(),
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body));

    private static (string MediaType, string Data) Base64Source(ContentBlockParam block) =>
        block.TryPickImage(out var image) && image.Source.TryPickBase64Image(out var source)
            ? (source.MediaType.Raw(), source.Data)
            : (null, null);

    private static (long Input, long Output, long? CacheWrite, long? CacheRead) TokensOf(Usage usage) =>
        (usage.InputTokens, usage.OutputTokens, usage.CacheCreationInputTokens, usage.CacheReadInputTokens);

    private static JsonElement EmbeddedSchema()
    {
        using var stream = typeof(AnthropicRecipePhotoReader).Assembly
            .GetManifestResourceStream("KCC.Web.Features.RecipeImport.ReaderOutputSchema.json");
        using var schema = JsonDocument.Parse(stream);
        return schema.RootElement.Clone();
    }

    private static HttpResponseMessage RecipeAnswer() => Answer("end_turn", ReaderOutputJson);

    private static HttpResponseMessage Answer(string stopReason, string text)
    {
        object[] content = text is null
            ? [new { type = "thinking", thinking = "The card lists the flour first.", signature = "sig" }]
            :
            [
                new { type = "thinking", thinking = "The card lists the flour first.", signature = "sig" },
                new { type = "text", text, citations = (object)null },
            ];

        return JsonAnswer(HttpStatusCode.OK, new
        {
            id = "msg_test",
            type = "message",
            role = "assistant",
            model = "claude-opus-5-5",
            content,
            stop_reason = stopReason,
            stop_sequence = (string)null,
            usage = new
            {
                input_tokens = 10_000,
                output_tokens = 2_000,
                cache_creation_input_tokens = 800,
                cache_read_input_tokens = 300,
            },
        });
    }

    private static HttpResponseMessage ErrorAnswer(HttpStatusCode status, string message) =>
        JsonAnswer(status, new { type = "error", error = new { type = "api_error", message } });

    private static HttpResponseMessage IconAnswer() => JsonAnswer(HttpStatusCode.OK, new
    {
        id = "msg_icon",
        type = "message",
        role = "assistant",
        model = "claude-haiku-4-5",
        content = new object[]
        {
            new
            {
                type = "tool_use",
                id = "toolu_test",
                name = "select_icon",
                input = new { icon = ModelIcon },
                caller = new { type = "direct" },
            },
        },
        stop_reason = "tool_use",
        stop_sequence = (string)null,
        usage = new { input_tokens = 120, output_tokens = 20 },
    });

    private static HttpResponseMessage JsonAnswer(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
    };

    private sealed class FakeAnthropic(Func<string, CancellationToken, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        private readonly ConcurrentQueue<string> bodies = new();
        private readonly TaskCompletionSource<CancellationToken> received =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<string> Bodies => [.. bodies];

        public int Calls => bodies.Count;

        public Task<CancellationToken> Received => received.Task;

        public static FakeAnthropic Answering(Func<HttpResponseMessage> response) =>
            new((_, _) => Task.FromResult(response()));

        public static FakeAnthropic Answering(Func<string, HttpResponseMessage> response) =>
            new((body, _) => Task.FromResult(response(body)));

        public static FakeAnthropic Failing() =>
            new((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("The connection was reset.")));

        public static FakeAnthropic Holding() => new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bodies.Enqueue(await request.Content.ReadAsStringAsync(cancellationToken));
            received.TrySetResult(cancellationToken);
            return await answer(bodies.Last(), cancellationToken);
        }
    }

    private sealed class RecordingLogger : ILogger<AnthropicRecipePhotoReader>
    {
        private readonly ConcurrentQueue<LogEntry> entries = new();

        public IReadOnlyList<LogEntry> Entries => [.. entries];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            entries.Enqueue(new LogEntry(
                logLevel,
                formatter(state, exception),
                state as IReadOnlyList<KeyValuePair<string, object>> ?? [],
                exception));
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyList<KeyValuePair<string, object>> Values,
        Exception Exception)
    {
        public object Value(string name) => Values.Single(value => value.Key == name).Value;
    }

    private sealed class ManualClock : TimeProvider
    {
        private readonly Lock gate = new();
        private readonly List<ManualTimer> timers = [];
        private TimeSpan elapsed;

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            lock (gate)
            {
                timers.Add(timer);
            }

            _ = timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan time)
        {
            ManualTimer[] due;
            lock (gate)
            {
                elapsed += time;
                due = [.. timers.Where(timer => timer.DueAt <= elapsed)];
            }

            foreach (var timer in due)
            {
                timer.Fire();
            }
        }

        private TimeSpan Elapsed
        {
            get
            {
                lock (gate)
                {
                    return elapsed;
                }
            }
        }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object state) : ITimer
        {
            public TimeSpan? DueAt { get; private set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : clock.Elapsed + dueTime;
                return true;
            }

            public void Fire()
            {
                DueAt = null;
                callback(state);
            }

            public void Dispose() => DueAt = null;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
