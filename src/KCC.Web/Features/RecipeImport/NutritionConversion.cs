namespace KCC.Web.Features.RecipeImport;

public static class NutritionConversion
{
    private const decimal KilojoulesPerKilocalorie = 4.184m;
    private const decimal SaltPerSodium = 2.5m;
    private const decimal MilligramsPerGram = 1_000m;
    private const decimal MaxCalories = 10_000m;
    private const decimal MaxGrams = 1_000m;
    private const decimal MaxSodiumMilligrams = 50_000m;
    private const string PerServingBasis = "serving";
    private const string WholeRecipeBasis = "recipe";

    private static readonly MarkedValue<int?> NoValue = new(null, null);
    private static readonly Mark OutOfRange = new(MarkKinds.Unclear, null, MarkCodes.OutOfRange, null);

    private static readonly UnitRatio Same = new(1m, 1m);

    // A ratio rather than one factor: 1 / 4.184 is inexact in decimal, and flooring a range's lower number after
    // multiplying by it lands one under the whole number an exact multiple of 4.184 kJ should give.
    private static readonly Dictionary<string, UnitRatio> Kilocalories =
        new(StringComparer.Ordinal) { ["kcal"] = Same, ["kJ"] = new(1m, KilojoulesPerKilocalorie) };

    private static readonly Dictionary<string, UnitRatio> Grams =
        new(StringComparer.Ordinal) { ["g"] = Same, ["mg"] = new(1m, MilligramsPerGram) };

    private static readonly Dictionary<string, UnitRatio> SodiumMilligrams =
        new(StringComparer.Ordinal) { ["mg"] = Same, ["g"] = new(MilligramsPerGram, 1m) };

    private static readonly Dictionary<string, UnitRatio> SaltAsSodiumMilligrams =
        new(StringComparer.Ordinal) { ["mg"] = new(1m, SaltPerSodium), ["g"] = new(MilligramsPerGram, SaltPerSodium) };

    public static NutritionValues PerServing(ReaderNutrition nutrition, MarkedValue<int?> servings)
    {
        if (Divisor(nutrition?.Basis, servings) is not { } divisor)
        {
            return Empty();
        }

        var printed = (nutrition.Values ?? [])
            .Where(value => value?.Amount is not null
                && !string.IsNullOrWhiteSpace(value.Nutrient)
                && !string.IsNullOrWhiteSpace(value.Printed))
            .GroupBy(value => value.Nutrient, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        MarkedValue<int?> Value(string nutrient, Dictionary<string, UnitRatio> factors, decimal limit) =>
            printed.TryGetValue(nutrient, out var value) ? Converted(value, factors, divisor, limit) : NoValue;

        var sodium = printed.ContainsKey("sodium")
            ? Value("sodium", SodiumMilligrams, MaxSodiumMilligrams)
            : Value("salt", SaltAsSodiumMilligrams, MaxSodiumMilligrams);
        return new NutritionValues(
            Value("calories", Kilocalories, MaxCalories),
            Value("protein", Grams, MaxGrams),
            Value("carbohydrates", Grams, MaxGrams),
            Value("fat", Grams, MaxGrams),
            Value("saturatedFat", Grams, MaxGrams),
            Value("fiber", Grams, MaxGrams),
            Value("sugar", Grams, MaxGrams),
            sodium);
    }

    public static NutritionValues Empty() => new(NoValue, NoValue, NoValue, NoValue, NoValue, NoValue, NoValue, NoValue);

    private static int? Divisor(string basis, MarkedValue<int?> servings) => basis switch
    {
        PerServingBasis => 1,
        WholeRecipeBasis when servings?.Value is > 0 && servings.Mark?.Kind != MarkKinds.Estimate => servings.Value,
        _ => null,
    };

    private static MarkedValue<int?> Converted(
        ReaderNutrient nutrient,
        Dictionary<string, UnitRatio> factors,
        int divisor,
        decimal limit)
    {
        if (nutrient.Unit is null || !factors.TryGetValue(nutrient.Unit, out var factor))
        {
            return NoValue;
        }

        if (Math.Abs(nutrient.Amount.Value) > decimal.MaxValue / factor.Times)
        {
            return new MarkedValue<int?>(null, OutOfRange);
        }

        var exact = nutrient.Amount.Value * factor.Times / factor.Per / divisor;
        var value = nutrient.Inexact ? decimal.Floor(exact) : decimal.Round(exact, 0, MidpointRounding.AwayFromZero);
        if (exact < 0 || value > limit)
        {
            return new MarkedValue<int?>(null, OutOfRange);
        }

        var printedAs = nutrient.Printed.Trim();
        var mark = nutrient.Inexact
            ? new Mark(MarkKinds.Unclear, null, MarkCodes.PrintedAs, printedAs)
            : value != exact || factor != Same || divisor != 1
                ? new Mark(MarkKinds.Calculated, null, MarkCodes.PrintedAs, printedAs)
                : null;
        return new MarkedValue<int?>((int)value, mark);
    }

    private readonly record struct UnitRatio(decimal Times, decimal Per);
}
