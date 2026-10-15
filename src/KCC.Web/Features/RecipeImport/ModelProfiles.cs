namespace KCC.Web.Features.RecipeImport;

public static class ModelProfiles
{
    private static readonly IReadOnlyDictionary<string, ModelProfile> ById = new ModelProfile[]
    {
        new(
            Id: "claude-haiku-4-5",
            Prices: new(
                InputUsdPerMTok: 1.00m, OutputUsdPerMTok: 5.00m, CacheWriteUsdPerMTok: 1.25m, CacheReadUsdPerMTok: 0.10m),
            UpperPrices: null,
            UpperPricesAbovePromptTokens: null,
            AdaptiveThinking: false,
            TakesEffort: false,
            MaxImageEdge: 1568,
            ImageTokenCap: 1_600),
        new(
            Id: "claude-haiku-5-5",
            Prices: new(
                InputUsdPerMTok: 0.10m, OutputUsdPerMTok: 0.50m, CacheWriteUsdPerMTok: 0.125m, CacheReadUsdPerMTok: 0.01m),
            UpperPrices: new(
                InputUsdPerMTok: 0.50m, OutputUsdPerMTok: 2.50m, CacheWriteUsdPerMTok: 0.625m, CacheReadUsdPerMTok: 0.05m),
            UpperPricesAbovePromptTokens: 100_000,
            AdaptiveThinking: true,
            TakesEffort: true,
            MaxImageEdge: 2576,
            ImageTokenCap: 4_784),
        new(
            Id: "claude-sonnet-5-5",
            Prices: new(
                InputUsdPerMTok: 2.00m, OutputUsdPerMTok: 10.00m, CacheWriteUsdPerMTok: 2.50m, CacheReadUsdPerMTok: 0.10m),
            UpperPrices: null,
            UpperPricesAbovePromptTokens: null,
            AdaptiveThinking: true,
            TakesEffort: true,
            MaxImageEdge: 2576,
            ImageTokenCap: 4_784),
        new(
            Id: "claude-opus-5-5",
            Prices: new(
                InputUsdPerMTok: 4.00m, OutputUsdPerMTok: 20.00m, CacheWriteUsdPerMTok: 5.00m, CacheReadUsdPerMTok: 0.20m),
            UpperPrices: null,
            UpperPricesAbovePromptTokens: null,
            AdaptiveThinking: true,
            TakesEffort: true,
            MaxImageEdge: 2576,
            ImageTokenCap: 4_784),
    }.ToDictionary(profile => profile.Id, StringComparer.Ordinal);

    public static ModelProfile Find(string modelId) =>
        modelId is not null && ById.TryGetValue(modelId, out var profile) ? profile : null;
}

public sealed record ModelProfile(
    string Id,
    PriceSet Prices,
    PriceSet UpperPrices,
    int? UpperPricesAbovePromptTokens,
    bool AdaptiveThinking,
    bool TakesEffort,
    int MaxImageEdge,
    int ImageTokenCap);

public sealed record PriceSet(
    decimal InputUsdPerMTok,
    decimal OutputUsdPerMTok,
    decimal CacheWriteUsdPerMTok,
    decimal CacheReadUsdPerMTok);
