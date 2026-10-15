using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Anthropic.Services;
using KCC.Web.Features.Models.Options;
using Microsoft.Extensions.Options;

namespace KCC.Web.Features.RecipeImport;

public sealed class AnthropicRecipePhotoReader(
    AnthropicClient client,
    AnthropicOptions anthropic,
    IOptionsMonitor<RecipeImportOptions> options,
    TimeProvider timeProvider,
    ILogger<AnthropicRecipePhotoReader> logger) : IRecipePhotoReader
{
    private const string ReadOutcome = "Read";

    private const string UsageLog =
        "Recipe photo read {Outcome} on {Model}: {InputTokens} input, {OutputTokens} output, "
        + "{CacheCreationInputTokens} cache-write and {CacheReadInputTokens} cache-read tokens, {CostMicroUsd} micro-USD.";

    private const string NoUsageLog = "Recipe photo read {Outcome} on {Model}, with no usage.";

    private const string NoProfileLog = "RecipeImport:Model {Model} has no price profile, so the photo import is off.";

    private static readonly IReadOnlyDictionary<string, JsonElement> Schema = LoadSchema();

    private static readonly JsonSerializerOptions ReaderOutputJson = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
    };

    // A retried read could be billed twice and settled once.
    private readonly IMessageService messages =
        client.Messages.WithOptions(clientOptions => clientOptions with { MaxRetries = 0 });

    public bool IsConfigured => !string.IsNullOrWhiteSpace(anthropic.ApiKey) && HasProfile(options.CurrentValue.Model);

    public async Task<ReadAttempt> ReadAsync(ReadRequest request, CancellationToken cancellationToken)
    {
        var current = options.CurrentValue;
        var profile = ModelProfiles.Find(current.Model)
            ?? throw new InvalidOperationException($"RecipeImport:Model {current.Model} has no price profile.");

        var attempt = await AttemptAsync(
            Parameters(request, current, profile),
            current.ReadTimeoutSeconds,
            cancellationToken);
        Log(attempt, current.Model, profile);
        return attempt;
    }

    private static Dictionary<string, JsonElement> LoadSchema()
    {
        using var schema = typeof(AnthropicRecipePhotoReader).Assembly.GetManifestResourceStream(
            $"{typeof(AnthropicRecipePhotoReader).Namespace}.ReaderOutputSchema.json");
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schema);
    }

    private static MessageCreateParams Parameters(ReadRequest request, RecipeImportOptions current, ModelProfile profile)
    {
        var format = new JsonOutputFormat { Schema = Schema };
        var parameters = new MessageCreateParams
        {
            Model = current.Model,
            MaxTokens = ReadCost.MaxTokens,
            System = ReaderPrompt.System,
            Messages = [new MessageParam { Role = Role.User, Content = Content(request) }],
            OutputConfig = profile.TakesEffort
                ? new OutputConfig { Format = format, Effort = current.Effort }
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

    private static ReadAttempt Interpret(Message message, Usage usage)
    {
        try
        {
            return message.StopReason?.Value() switch
            {
                StopReason.Refusal => ReadAttempt.Refused(usage),
                StopReason.MaxTokens => ReadAttempt.CutOff(usage),
                _ => ReaderOutputOf(message) is { } output ? ReadAttempt.Read(output, usage) : ReadAttempt.Unusable(usage),
            };
        }
        catch (Exception exception) when (exception is AnthropicInvalidDataException or JsonException)
        {
            return ReadAttempt.Unusable(usage);
        }
    }

    private static ReaderOutput ReaderOutputOf(Message message)
    {
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var text))
            {
                return JsonSerializer.Deserialize<ReaderOutput>(text.Text, ReaderOutputJson);
            }
        }

        return null;
    }

    private async Task<ReadAttempt> AttemptAsync(
        MessageCreateParams parameters,
        int readTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(readTimeoutSeconds), timeProvider);
        using var read = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);

        Message message;
        Usage usage;
        try
        {
            // The SDK stops passing the token on once the headers arrive.
            using var response = await messages.WithRawResponse.Create(parameters, read.Token);
            message = await response.Deserialize(read.Token).WaitAsync(read.Token);
            usage = message.Usage;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReadAttempt.TimedOut();
        }
        catch (AnthropicApiException)
        {
            return ReadAttempt.ServiceError();
        }
        catch (Exception exception)
            when (exception is AnthropicException or IOException or HttpRequestException or JsonException)
        {
            return ReadAttempt.NoAnswer();
        }

        return Interpret(message, usage);
    }

    private bool HasProfile(string model)
    {
        if (ModelProfiles.Find(model) is not null)
        {
            return true;
        }

        logger.LogError(NoProfileLog, model);
        return false;
    }

    private void Log(ReadAttempt attempt, string model, ModelProfile profile)
    {
        var outcome = attempt.Failure?.ToString() ?? ReadOutcome;
        if (attempt.Usage is not { } usage)
        {
            logger.LogWarning(NoUsageLog, outcome, model);
            return;
        }

        logger.Log(
            attempt.Failure is null ? LogLevel.Information : LogLevel.Warning,
            UsageLog,
            outcome,
            model,
            usage.InputTokens,
            usage.OutputTokens,
            usage.CacheCreationInputTokens ?? 0,
            usage.CacheReadInputTokens ?? 0,
            ReadCost.MicroUsd(profile, usage));
    }
}
