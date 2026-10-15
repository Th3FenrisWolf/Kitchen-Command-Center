using System.Globalization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.RecipeImport;

public interface IImportSpendLedger
{
    long TotalMicroUsd(string month);

    bool TryReserve(string month, long microUsd, out SpendReservation reservation);

    void Settle(SpendReservation reservation, long settledMicroUsd);
}

public sealed class ImportSpendLedger(
    ICoreScopeProvider scopeProvider,
    IKeyValueService keyValues,
    IOptionsMonitor<RecipeImportOptions> options) : IImportSpendLedger
{
    private const decimal MicroUsdPerUsd = 1_000_000m;

    public static string MonthOf(DateTimeOffset moment) =>
        moment.UtcDateTime.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static bool IsUnderCeiling(long totalMicroUsd, RecipeImportOptions options) =>
        totalMicroUsd < options.MonthlyCeilingUsd * MicroUsdPerUsd;

    public long TotalMicroUsd(string month) =>
        keyValues.GetValue(Key(month)) is { } total ? long.Parse(total, CultureInfo.InvariantCulture) : 0;

    public bool TryReserve(string month, long microUsd, out SpendReservation reservation)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.KeyValues);
        var total = TotalMicroUsd(month);
        if (!IsUnderCeiling(total, options.CurrentValue))
        {
            reservation = null;
            return false;
        }

        Write(month, total + microUsd);
        scope.Complete();
        reservation = new SpendReservation(month, microUsd);
        return true;
    }

    public void Settle(SpendReservation reservation, long settledMicroUsd)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.KeyValues);
        Write(reservation.Month, TotalMicroUsd(reservation.Month) + settledMicroUsd - reservation.ReservedMicroUsd);
        scope.Complete();
    }

    private static string Key(string month) => $"KCC.RecipeImport.SpendMicroUsd.{month}";

    private void Write(string month, long totalMicroUsd) =>
        keyValues.SetValue(Key(month), totalMicroUsd.ToString(CultureInfo.InvariantCulture));
}

public sealed record SpendReservation(string Month, long ReservedMicroUsd);
