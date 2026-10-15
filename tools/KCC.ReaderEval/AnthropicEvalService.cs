using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KCC.ReaderEval;

public sealed class AnthropicEvalService(
    AnthropicClient client,
    AnthropicOptions anthropic,
    ServiceErrorRecorder recorder,
    TimeProvider timeProvider) : IEvalService
{
    private static readonly IReadOnlyDictionary<string, JsonElement> Schema = LoadSchema();

    private readonly Dictionary<string, AnthropicRecipePhotoReader> readers = [];

    public async Task<TimedAttempt> ReadAsync(
        string model,
        IReadOnlyList<ReadPhoto> photos,
        RecipeCatalog catalog,
        CancellationToken cancellationToken)
    {
        var reader = ReaderFor(model);
        recorder.Clear();
        var started = timeProvider.GetTimestamp();
        var attempt = await reader.ReadAsync(Request(photos) with { Catalog = catalog }, cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(started);
        return new TimedAttempt(attempt, elapsed, attempt.Failure == ReadFailure.ServiceError ? recorder.LastError : null);
    }

    public async Task<TokenCount> CountTokensAsync(
        string model,
        IReadOnlyList<ReadPhoto> photos,
        RecipeCatalog catalog,
        CancellationToken cancellationToken)
    {
        recorder.Clear();
        try
        {
            var count = await client.Messages.CountTokens(CountParameters(model, Request(photos) with { Catalog = catalog }), cancellationToken);
            return new TokenCount(count.InputTokens, null);
        }
        catch (AnthropicApiException)
        {
            return new TokenCount(null, recorder.LastError ?? "The service refused the request.");
        }
        catch (Exception exception) when (exception is AnthropicException or HttpRequestException or IOException)
        {
            return new TokenCount(null, $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static ReadRequest Request(IReadOnlyList<ReadPhoto> photos) => new(ImportFlows.Recipe, null, photos, null);

    private static MessageCountTokensParams CountParameters(string model, ReadRequest request)
    {
        var profile = ModelProfiles.Find(model);
        var format = new JsonOutputFormat { Schema = Schema };
        var parameters = new MessageCountTokensParams
        {
            Model = model,
            System = ReaderPrompt.System,
            Messages = [new MessageParam { Role = Role.User, Content = Content(request) }],
            OutputConfig = profile.TakesEffort
                ? new OutputConfig { Format = format, Effort = new RecipeImportOptions().Effort }
                : new OutputConfig { Format = format },
        };

        return profile.AdaptiveThinking ? parameters with { Thinking = new ThinkingConfigAdaptive() } : parameters;
    }

    private static List<ContentBlockParam> Content(ReadRequest request) =>
    [
        .. request.Photos.Select(photo => (ContentBlockParam)new ImageBlockParam(new Base64ImageSource
        {
            Data = Convert.ToBase64String(photo.Data),
            MediaType = photo.MediaType,
        })),
        new TextBlockParam { Text = ReaderPrompt.Instruction(request) },
    ];

    private static Dictionary<string, JsonElement> LoadSchema()
    {
        var readerType = typeof(AnthropicRecipePhotoReader);
        using var schema = readerType.Assembly.GetManifestResourceStream($"{readerType.Namespace}.ReaderOutputSchema.json");
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schema);
    }

    private AnthropicRecipePhotoReader ReaderFor(string model)
    {
        if (!readers.TryGetValue(model, out var reader))
        {
            reader = new AnthropicRecipePhotoReader(
                client,
                anthropic,
                new FixedOptions(new RecipeImportOptions { Model = model }),
                timeProvider,
                NullLogger<AnthropicRecipePhotoReader>.Instance);
            readers[model] = reader;
        }

        return reader;
    }

    private sealed class FixedOptions(RecipeImportOptions value) : IOptionsMonitor<RecipeImportOptions>
    {
        public RecipeImportOptions CurrentValue => value;

        public RecipeImportOptions Get(string name) => value;

        public IDisposable OnChange(Action<RecipeImportOptions, string> listener) => null;
    }
}
