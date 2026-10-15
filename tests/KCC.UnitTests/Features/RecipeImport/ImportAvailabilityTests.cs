using System.Globalization;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.Options;
using Moq;

namespace KCC.UnitTests.Features.RecipeImport;

public class ImportAvailabilityTests
{
    private const string Now = "2026-10-12T15:00:00Z";
    private const string ThisMonth = "2026-10";
    private const long OneDollar = 1_000_000;

    [Test]
    public async Task Enabled_FollowsWhetherTheRegisteredReaderIsConfigured()
    {
        var configured = false;
        var reader = new Mock<IRecipePhotoReader>();
        reader.Setup(registered => registered.IsConfigured).Returns(() => configured);
        var availability = Availability(reader.Object, Ledger(), Options(), Clock(Now));

        var beforeConfigured = await availability.GetAsync();
        configured = true;
        var afterConfigured = await availability.GetAsync();

        _ = await Assert.That(beforeConfigured.Enabled).IsFalse();
        _ = await Assert.That(afterConfigured.Enabled).IsTrue();
    }

    [Test]
    public async Task AReaderThatIsNotConfigured_LeavesTheImportUnavailable()
    {
        var availability = Availability(Reader(configured: false), Ledger(), Options(), Clock(Now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.Available).IsFalse();
    }

    [Test]
    public async Task UnderTheCeiling_TheImportIsAvailable_WithNoDateToComeBackOn()
    {
        var availability = Availability(Reader(), Ledger(ThisMonth, OneDollar - 1), Options(ceilingUsd: 1.00m), Clock(Now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.Enabled).IsTrue();
        _ = await Assert.That(import.Available).IsTrue();
        _ = await Assert.That(import.AvailableAgainOn).IsNull();
    }

    [Test]
    [Arguments(OneDollar)]
    [Arguments(OneDollar + 250_000)]
    public async Task AtOrAboveTheCeiling_TheImportIsUnavailable(long monthTotal)
    {
        var availability = Availability(Reader(), Ledger(ThisMonth, monthTotal), Options(ceilingUsd: 1.00m), Clock(Now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.Enabled).IsTrue();
        _ = await Assert.That(import.Available).IsFalse();
    }

    [Test]
    public async Task RaisingTheCeilingAboveTheMonthsTotal_MakesTheImportAvailableAgain()
    {
        var options = new RecipeImportOptions { MonthlyCeilingUsd = 1.00m };
        var availability = Availability(Reader(), Ledger(ThisMonth, OneDollar + 250_000), Options(options), Clock(Now));

        var atTheOldCeiling = await availability.GetAsync();
        options.MonthlyCeilingUsd = 2.00m;
        var underTheNewCeiling = await availability.GetAsync();

        _ = await Assert.That(atTheOldCeiling.Available).IsFalse();
        _ = await Assert.That(underTheNewCeiling.Available).IsTrue();
        _ = await Assert.That(underTheNewCeiling.AvailableAgainOn).IsNull();
    }

    [Test]
    [Arguments("2026-10-12T15:00:00Z", "2026-10", "2026-11-01")]
    [Arguments("2026-12-01T00:00:00Z", "2026-12", "2027-01-01")]
    [Arguments("2026-12-31T23:59:59Z", "2026-12", "2027-01-01")]
    [Arguments("2027-01-01T01:00:00Z", "2027-01", "2027-02-01")]
    public async Task AvailableAgainOn_IsTheFirstDayOfTheNextUtcMonth(string now, string utcMonth, string expected)
    {
        var availability = Availability(Reader(), Ledger(utcMonth, OneDollar), Options(ceilingUsd: 1.00m), Clock(now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.Available).IsFalse();
        _ = await Assert.That(import.AvailableAgainOn).IsEqualTo(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Test]
    [Arguments("claude-opus-5-5", 2000, 2000)]
    [Arguments("claude-opus-5-5", 3000, 2576)]
    [Arguments("claude-sonnet-5-5", 3000, 2576)]
    [Arguments("claude-haiku-5-5", 3000, 2576)]
    [Arguments("claude-haiku-4-5", 2000, 1568)]
    public async Task MaxImageEdge_IsTheLowerOfTheOptionAndTheModelsEdge(string model, int optionEdge, int expected)
    {
        var options = new RecipeImportOptions { Model = model, MaxImageEdge = optionEdge };
        var availability = Availability(Reader(), Ledger(), Options(options), Clock(Now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.MaxImageEdge).IsEqualTo(expected);
    }

    [Test]
    public async Task MaxImageEdge_ForAModelWithNoProfile_IsTheOptionAlone()
    {
        var options = new RecipeImportOptions { Model = "claude-opus-4-1", MaxImageEdge = 3000 };
        var availability = Availability(Reader(configured: false), Ledger(), Options(options), Clock(Now));

        var import = await availability.GetAsync();

        _ = await Assert.That(import.MaxImageEdge).IsEqualTo(3000);
    }

    private static ImportAvailability Availability(
        IRecipePhotoReader reader,
        IImportSpendLedger ledger,
        IOptionsMonitor<RecipeImportOptions> options,
        TimeProvider clock) => new(reader, ledger, options, clock);

    private static IRecipePhotoReader Reader(bool configured = true) =>
        Mock.Of<IRecipePhotoReader>(reader => reader.IsConfigured == configured);

    private static IImportSpendLedger Ledger(string month = ThisMonth, long totalMicroUsd = 0)
    {
        var ledger = new Mock<IImportSpendLedger>();
        ledger.Setup(spend => spend.TotalMicroUsd(month)).Returns(totalMicroUsd);
        return ledger.Object;
    }

    private static IOptionsMonitor<RecipeImportOptions> Options(decimal ceilingUsd = 2.00m) =>
        Options(new RecipeImportOptions { MonthlyCeilingUsd = ceilingUsd });

    private static IOptionsMonitor<RecipeImportOptions> Options(RecipeImportOptions options) =>
        Mock.Of<IOptionsMonitor<RecipeImportOptions>>(monitor => monitor.CurrentValue == options);

    private static TimeProvider Clock(string utcNow) => new FixedClock(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture));

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        // Near the turn of a month, a month read from this local time is the wrong one.
        private static readonly TimeZoneInfo TwelveHoursBehindUtc =
            TimeZoneInfo.CreateCustomTimeZone("UTC-12", TimeSpan.FromHours(-12), "UTC-12", "UTC-12");

        public override TimeZoneInfo LocalTimeZone => TwelveHoursBehindUtc;

        public override DateTimeOffset GetUtcNow() => utcNow.ToUniversalTime();
    }
}
