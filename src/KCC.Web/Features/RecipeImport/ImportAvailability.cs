using Microsoft.Extensions.Options;

namespace KCC.Web.Features.RecipeImport;

public sealed class ImportAvailability(
    IRecipePhotoReader reader,
    IImportSpendLedger ledger,
    IOptionsMonitor<RecipeImportOptions> options,
    TimeProvider timeProvider)
{
    public static DateOnly AvailableAgainOn(DateTimeOffset moment)
    {
        var utc = moment.UtcDateTime;
        return new DateOnly(utc.Year, utc.Month, 1).AddMonths(1);
    }

    public Task<ImportAvailabilityModel> GetAsync()
    {
        var current = options.CurrentValue;
        var now = timeProvider.GetUtcNow();
        var enabled = reader.IsConfigured;
        var available = enabled
            && ImportSpendLedger.IsUnderCeiling(ledger.TotalMicroUsd(ImportSpendLedger.MonthOf(now)), current);

        return Task.FromResult(new ImportAvailabilityModel(
            enabled,
            available,
            available ? null : AvailableAgainOn(now),
            MaxImageEdge(current)));
    }

    private static int MaxImageEdge(RecipeImportOptions options) =>
        ModelProfiles.Find(options.Model) is { } profile
            ? Math.Min(options.MaxImageEdge, profile.MaxImageEdge)
            : options.MaxImageEdge;
}

public sealed record ImportAvailabilityModel(bool Enabled, bool Available, DateOnly? AvailableAgainOn, int MaxImageEdge);
