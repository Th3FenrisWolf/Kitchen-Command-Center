using Anthropic.Models.Messages;

namespace KCC.Web.Features.RecipeImport;

public static class ReadCost
{
    public const int MaxTokens = 12_000;

    private const int FixedPromptTokens = 3_000;
    private const decimal CatalogCharactersPerToken = 3.5m;
    private const decimal TokensPerMTok = 1_000_000m;
    private const decimal MicroUsdPerUsd = 1_000_000m;

    public static long MicroUsd(ModelProfile profile, Usage usage)
    {
        long cacheWriteTokens = usage.CacheCreationInputTokens ?? 0;
        long cacheReadTokens = usage.CacheReadInputTokens ?? 0;
        var prices = PricesFor(profile, usage.InputTokens + cacheWriteTokens + cacheReadTokens);

        return RoundedUpMicroUsd(
            Usd(usage.InputTokens, prices.InputUsdPerMTok)
            + Usd(usage.OutputTokens, prices.OutputUsdPerMTok)
            + Usd(cacheWriteTokens, prices.CacheWriteUsdPerMTok)
            + Usd(cacheReadTokens, prices.CacheReadUsdPerMTok));
    }

    public static long ReservationMicroUsd(ModelProfile profile, int photoCount, int catalogCharacters)
    {
        var prices = profile.UpperPrices ?? profile.Prices;
        decimal promptTokens =
            FixedPromptTokens
            + (catalogCharacters / CatalogCharactersPerToken)
            + (photoCount * profile.ImageTokenCap);

        return RoundedUpMicroUsd(Usd(promptTokens, prices.InputUsdPerMTok) + Usd(MaxTokens, prices.OutputUsdPerMTok));
    }

    private static PriceSet PricesFor(ModelProfile profile, long promptTokens) =>
        profile.UpperPrices is { } upperPrices && promptTokens > profile.UpperPricesAbovePromptTokens
            ? upperPrices
            : profile.Prices;

    private static decimal Usd(decimal tokens, decimal usdPerMTok) => (tokens * usdPerMTok) / TokensPerMTok;

    private static long RoundedUpMicroUsd(decimal usd) => (long)decimal.Ceiling(usd * MicroUsdPerUsd);
}
