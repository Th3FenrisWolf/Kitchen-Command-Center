using KCC.IntegrationTests.Config;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.RecipeImport;

public class ImportSpendLedgerTests
{
    private static readonly long Ceiling = (long)(UmbracoSite.ImportCeilingUsd * 1_000_000m);
    private static readonly TimeSpan StallLimit = TimeSpan.FromSeconds(20);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IImportSpendLedger Ledger => Site.Services.GetRequiredService<IImportSpendLedger>();

    [Test]
    public async Task AReservationBelowTheCeiling_IsGrantedWhateverItsSize_AndAddsItselfToTheMonth()
    {
        const string month = "1990-01";
        TestImportSpend.Set(Site.Services, month, Ceiling - 1);

        var granted = Ledger.TryReserve(month, 3 * Ceiling, out var reservation);

        _ = await Assert.That(granted).IsTrue();
        _ = await Assert.That(reservation).IsEqualTo(new SpendReservation(month, 3 * Ceiling));
        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo((4 * Ceiling) - 1);
    }

    [Test]
    [Arguments(0L)]
    [Arguments(250_000L)]
    public async Task AReservationAtOrAboveTheCeiling_IsRefusedAndChangesNothing(long overTheCeiling)
    {
        const string month = "1990-02";
        TestImportSpend.Set(Site.Services, month, Ceiling + overTheCeiling);

        var granted = Ledger.TryReserve(month, 100_000, out var reservation);

        _ = await Assert.That(granted).IsFalse();
        _ = await Assert.That(reservation).IsNull();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(Ceiling + overTheCeiling);
    }

    [Test]
    public async Task Settling_AddsTheSettledCostMinusTheReservationToItsMonth()
    {
        const string month = "1990-03";
        TestImportSpend.Set(Site.Services, month, 100_000);
        _ = Ledger.TryReserve(month, 300_000, out var reservation);

        Ledger.Settle(reservation, 120_000);

        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(220_000L);
    }

    [Test]
    public async Task SettlingAfterTheMonthEnds_ChangesTheReservationsMonthOnly()
    {
        const string endedMonth = "1990-04";
        const string nextMonth = "1990-05";
        _ = Ledger.TryReserve(endedMonth, 300_000, out var lastReadOfTheMonth);
        _ = Ledger.TryReserve(nextMonth, 200_000, out _);

        Ledger.Settle(lastReadOfTheMonth, 50_000);

        _ = await Assert.That(TestImportSpend.Read(Site.Services, endedMonth)).IsEqualTo(50_000L);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, nextMonth)).IsEqualTo(200_000L);
    }

    [Test]
    public async Task SettlingZero_GivesTheWholeReservationBack()
    {
        const string month = "1990-06";
        TestImportSpend.Set(Site.Services, month, 250_000);
        _ = Ledger.TryReserve(month, 300_000, out var reservation);

        Ledger.Settle(reservation, 0);

        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(250_000L);
    }

    [Test]
    public async Task AMissingKey_ReadsAsZero()
    {
        const string month = "1990-07";
        var stored = TestImportSpend.Read(Site.Services, month);

        var total = Ledger.TotalMicroUsd(month);
        var granted = Ledger.TryReserve(month, 400_000, out _);

        _ = await Assert.That(stored).IsNull();
        _ = await Assert.That(total).IsEqualTo(0L);
        _ = await Assert.That(granted).IsTrue();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(400_000L);
    }

    [Test]
    public async Task TwentyParallelReservations_FinishWithoutAStall_AndTotalTheGrantedOnes()
    {
        const string month = "1990-08";
        const long reservationSize = 150_000;

        var reservations = Enumerable.Repeat(month, 20)
            .Select(sameMonth => Task.Run(() => Ledger.TryReserve(sameMonth, reservationSize, out _)))
            .ToList();
        var granted = (await Task.WhenAll(reservations).WaitAsync(StallLimit)).Count(isGranted => isGranted);

        _ = await Assert.That(granted).IsEqualTo(7);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(granted * reservationSize);
    }

    [Test]
    public async Task TwentyParallelSettlements_FinishWithoutAStall_AndTotalTheSettledCosts()
    {
        const string month = "1990-09";
        const long reservationSize = 40_000;
        const long settledCost = 25_000;
        var reservations = Enumerable.Repeat(month, 20)
            .Select(sameMonth => Ledger.TryReserve(sameMonth, reservationSize, out var reservation) ? reservation : null)
            .OfType<SpendReservation>()
            .ToList();

        var settlements = reservations
            .Select(reservation => Task.Run(() => Ledger.Settle(reservation, settledCost)))
            .ToList();
        await Task.WhenAll(settlements).WaitAsync(StallLimit);

        _ = await Assert.That(reservations.Count).IsEqualTo(20);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, month)).IsEqualTo(20 * settledCost);
    }
}
