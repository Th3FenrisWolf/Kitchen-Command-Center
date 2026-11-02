using Anthropic.Models.Messages;
using KCC.Web.Features.RecipeImport;

namespace KCC.UnitTests.Features.RecipeImport;

public class ReadCostTests
{
    [Test]
    [Arguments("claude-haiku-4-5", 10_000L, 2_000L, 800L, 300L, 21_030L)]
    [Arguments("claude-haiku-5-5", 10_000L, 2_000L, 800L, 300L, 2_103L)]
    [Arguments("claude-haiku-5-5", 200_000L, 2_000L, 800L, 300L, 105_515L)]
    [Arguments("claude-sonnet-5-5", 10_000L, 2_000L, 800L, 300L, 42_030L)]
    [Arguments("claude-opus-5-5", 10_000L, 2_000L, 800L, 300L, 84_060L)]
    public async Task MicroUsd_PricesEachKindOfTokenAtTheProfilesRates(
        string model,
        long input,
        long output,
        long cacheWrite,
        long cacheRead,
        long expected)
    {
        var cost = ReadCost.MicroUsd(ModelProfiles.Find(model), UsageOf(input, output, cacheWrite, cacheRead));

        _ = await Assert.That(cost).IsEqualTo(expected);
    }

    [Test]
    [Arguments("claude-haiku-4-5", 211_030L)]
    [Arguments("claude-sonnet-5-5", 422_030L)]
    [Arguments("claude-opus-5-5", 844_060L)]
    public async Task MicroUsd_AModelWithOnePriceSet_KeepsItAbove100000PromptTokens(string model, long expected)
    {
        var cost = ReadCost.MicroUsd(ModelProfiles.Find(model), UsageOf(200_000, 2_000, 800, 300));

        _ = await Assert.That(cost).IsEqualTo(expected);
    }

    [Test]
    public async Task MicroUsd_HaikuFiveFive_SettlesAtItsFirstPriceSetAt100000PromptTokens()
    {
        var usage = UsageOf(input: 60_000, output: 1_000, cacheWrite: 30_000, cacheRead: 10_000);

        var cost = ReadCost.MicroUsd(ModelProfiles.Find("claude-haiku-5-5"), usage);

        _ = await Assert.That(cost).IsEqualTo(10_350L);
    }

    [Test]
    public async Task MicroUsd_HaikuFiveFive_SettlesAtItsHigherPriceSetAt100001PromptTokens()
    {
        var usage = UsageOf(input: 60_000, output: 1_000, cacheWrite: 30_000, cacheRead: 10_001);

        var cost = ReadCost.MicroUsd(ModelProfiles.Find("claude-haiku-5-5"), usage);

        _ = await Assert.That(cost).IsEqualTo(51_751L);
    }

    [Test]
    public async Task MicroUsd_RoundsAFractionOfAMicroDollarUp()
    {
        var cost = ReadCost.MicroUsd(ModelProfiles.Find("claude-haiku-5-5"), UsageOf(1, 0, 0, 0));

        _ = await Assert.That(cost).IsEqualTo(1L);
    }

    [Test]
    public async Task MicroUsd_CountsAbsentCacheTokensAsNone()
    {
        var cost = ReadCost.MicroUsd(ModelProfiles.Find("claude-opus-5-5"), UsageOf(10_000, 2_000, null, null));

        _ = await Assert.That(cost).IsEqualTo(80_000L);
    }

    [Test]
    [Arguments("claude-opus-4-1")]
    [Arguments("claude-haiku-4-5-20251001")]
    [Arguments("")]
    [Arguments((string)null)]
    public async Task Find_GivesAModelOutsideTheTableNoProfile(string model)
    {
        _ = await Assert.That(ModelProfiles.Find(model)).IsNull();
    }

    [Test]
    [Arguments("claude-haiku-4-5", 3, 7_000, 71_300L)]
    [Arguments("claude-haiku-5-5", 3, 7_000, 40_426L)]
    [Arguments("claude-sonnet-5-5", 3, 7_000, 161_704L)]
    [Arguments("claude-opus-5-5", 3, 7_000, 323_408L)]
    [Arguments("claude-opus-5-5", 1, 0, 271_136L)]
    [Arguments("claude-haiku-4-5", 1, 1, 64_601L)]
    public async Task ReservationMicroUsd_FollowsTheSpendReservationEstimate(
        string model,
        int photoCount,
        int catalogCharacters,
        long expected)
    {
        var reservation = ReadCost.ReservationMicroUsd(ModelProfiles.Find(model), photoCount, catalogCharacters);

        _ = await Assert.That(reservation).IsEqualTo(expected);
    }

    [Test]
    public async Task ReservationMicroUsd_HaikuFiveFive_PricesAtItsHigherSetWhateverTheEstimate()
    {
        var profile = ModelProfiles.Find("claude-haiku-5-5");

        var reservation = ReadCost.ReservationMicroUsd(profile, photoCount: 1, catalogCharacters: 0);

        _ = await Assert.That(reservation).IsEqualTo(33_892L);
    }

    private static Usage UsageOf(long input, long output, long? cacheWrite, long? cacheRead) => new()
    {
        InputTokens = input,
        OutputTokens = output,
        CacheCreationInputTokens = cacheWrite,
        CacheReadInputTokens = cacheRead,
        CacheCreation = null,
        InferenceGeo = null,
        OutputTokensDetails = null,
        ServerToolUse = null,
        ServiceTier = null,
    };
}
