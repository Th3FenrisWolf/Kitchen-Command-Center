using KCC.Web.Features.RecipeImport;

namespace KCC.UnitTests.Features.RecipeImport;

public class NutritionConversionTests
{
    private static readonly MarkedValue<int?> FourServings = new(4, null);
    private static readonly MarkedValue<int?> NoValue = new(null, null);
    private static readonly Mark OutOfRange = new(MarkKinds.Unclear, null, MarkCodes.OutOfRange, null);

    [Test]
    public async Task AWholePerServingValue_CopiesUnmarked()
    {
        var values = NutritionConversion.PerServing(
            PerServing(
                Printed("calories", 350m, "kcal", "350 kcal"),
                Printed("protein", 12m, "g", "12 g"),
                Printed("carbohydrates", 40m, "g", "40 g"),
                Printed("fat", 9m, "g", "9 g"),
                Printed("saturatedFat", 0m, "g", "0 g"),
                Printed("fiber", 3m, "g", "3 g"),
                Printed("sugar", 5m, "g", "5 g"),
                Printed("sodium", 480m, "mg", "480 mg")),
            FourServings);

        _ = await Assert.That(values).IsEqualTo(new NutritionValues(
            Unmarked(350),
            Unmarked(12),
            Unmarked(40),
            Unmarked(9),
            Unmarked(0),
            Unmarked(3),
            Unmarked(5),
            Unmarked(480)));
    }

    [Test]
    [Arguments("1.5", 2)]
    [Arguments("2.5", 3)]
    [Arguments("2.49", 2)]
    [Arguments("0.5", 1)]
    public async Task APerServingValueWithAFraction_RoundsHalfUp_AndIsCalculatedFromItsPrintedText(string amount, int rounded)
    {
        var printed = $"{amount} g";

        var values = NutritionConversion.PerServing(
            PerServing(Printed("fiber", decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "g", printed)),
            FourServings);

        _ = await Assert.That(values.FiberG).IsEqualTo(Calculated(rounded, printed));
    }

    [Test]
    public async Task Kilojoules_BecomeKilocaloriesAt4Point184()
    {
        var values = NutritionConversion.PerServing(PerServing(Printed("calories", 1200m, "kJ", "1200 kJ")), FourServings);

        _ = await Assert.That(values.Calories).IsEqualTo(Calculated(287, "1200 kJ"));
    }

    [Test]
    public async Task SodiumInGrams_BecomesMilligrams()
    {
        var values = NutritionConversion.PerServing(PerServing(Printed("sodium", 0.4m, "g", "0.4 g")), FourServings);

        _ = await Assert.That(values.SodiumMg).IsEqualTo(Calculated(400, "0.4 g"));
    }

    [Test]
    public async Task SaltWithNoSodiumPrinted_DividesBy2Point5()
    {
        var values = NutritionConversion.PerServing(PerServing(Printed("salt", 1.2m, "g", "Salt 1.2 g")), FourServings);

        _ = await Assert.That(values.SodiumMg).IsEqualTo(Calculated(480, "Salt 1.2 g"));
    }

    [Test]
    public async Task PrintedSodium_WinsOverSalt()
    {
        var values = NutritionConversion.PerServing(
            PerServing(Printed("salt", 1.2m, "g", "Salt 1.2 g"), Printed("sodium", 450m, "mg", "450 mg")),
            FourServings);

        _ = await Assert.That(values.SodiumMg).IsEqualTo(Unmarked(450));
    }

    [Test]
    public async Task AWholeRecipePanel_DividesByThePrintedServings()
    {
        var values = NutritionConversion.PerServing(
            new ReaderNutrition("recipe", [Printed("calories", 2000m, "kcal", "2000 kcal"), Printed("protein", 50m, "g", "50 g")]),
            new MarkedValue<int?>(8, null));

        _ = await Assert.That(values.Calories).IsEqualTo(Calculated(250, "2000 kcal"));
        _ = await Assert.That(values.ProteinG).IsEqualTo(Calculated(6, "50 g"));
    }

    [Test]
    public async Task AWholeRecipePanelInKilojoulesWithSalt_ConvertsThenDivides()
    {
        var values = NutritionConversion.PerServing(
            new ReaderNutrition("recipe", [Printed("calories", 8000m, "kJ", "8000 kJ"), Printed("salt", 10m, "g", "10 g salt")]),
            new MarkedValue<int?>(8, null));

        _ = await Assert.That(values.Calories).IsEqualTo(Calculated(239, "8000 kJ"));
        _ = await Assert.That(values.SodiumMg).IsEqualTo(Calculated(500, "10 g salt"));
    }

    [Test]
    public async Task AWholeRecipePanelWithNoServings_LeavesEveryValueEmpty()
    {
        var values = NutritionConversion.PerServing(
            new ReaderNutrition("recipe", [Printed("calories", 2000m, "kcal", "2000 kcal")]),
            NoValue);

        _ = await Assert.That(values).IsEqualTo(Empty());
    }

    [Test]
    public async Task AWholeRecipePanelWithEstimatedServings_LeavesEveryValueEmpty()
    {
        var values = NutritionConversion.PerServing(
            new ReaderNutrition("recipe", [Printed("calories", 2000m, "kcal", "2000 kcal")]),
            new MarkedValue<int?>(4, new Mark(MarkKinds.Estimate, "Not printed", null, null)));

        _ = await Assert.That(values).IsEqualTo(Empty());
    }

    [Test]
    public async Task APanelWithNoBasis_LeavesEveryValueEmptyAndUnmarked()
    {
        var values = NutritionConversion.PerServing(
            new ReaderNutrition(null, [Printed("calories", 350m, "kcal", "350 kcal"), Printed("fiber", 1.5m, "g", "1.5 g")]),
            FourServings);

        _ = await Assert.That(values).IsEqualTo(Empty());
    }

    [Test]
    public async Task NoPanel_LeavesEveryValueEmpty()
    {
        _ = await Assert.That(NutritionConversion.PerServing(null, FourServings)).IsEqualTo(Empty());
    }

    [Test]
    [Arguments("2.7", "2.7-4 g", 2)]
    [Arguments("0", "<1 g", 0)]
    [Arguments("3", "3 to 4 g", 3)]
    public async Task ARangeOrLessThan_KeepsTheFloorOfItsLowerNumber_AndIsUnclear(string lower, string printed, int floor)
    {
        var values = NutritionConversion.PerServing(
            PerServing(new ReaderNutrient("sugar", decimal.Parse(lower, System.Globalization.CultureInfo.InvariantCulture), "g", printed, Inexact: true)),
            FourServings);

        _ = await Assert.That(values.SugarG)
            .IsEqualTo(new MarkedValue<int?>(floor, new Mark(MarkKinds.Unclear, null, MarkCodes.PrintedAs, printed)));
    }

    [Test]
    [Arguments("2092", "2092-2500 kJ", 500)]
    [Arguments("4184", "4184-5000 kJ", 1000)]
    public async Task AKilojouleRange_ThatConvertsToAWholeNumber_KeepsThatNumber(string lower, string printed, int floor)
    {
        var values = NutritionConversion.PerServing(
            PerServing(new ReaderNutrient("calories", decimal.Parse(lower, System.Globalization.CultureInfo.InvariantCulture), "kJ", printed, Inexact: true)),
            FourServings);

        _ = await Assert.That(values.Calories)
            .IsEqualTo(new MarkedValue<int?>(floor, new Mark(MarkKinds.Unclear, null, MarkCodes.PrintedAs, printed)));
    }

    [Test]
    [Arguments("calories", "10001", "kcal")]
    [Arguments("protein", "1001", "g")]
    [Arguments("sodium", "50001", "mg")]
    [Arguments("fat", "-1", "g")]
    public async Task AResultOverItsLimit_IsEmptyAndOutOfRange(string nutrient, string amount, string unit)
    {
        var values = NutritionConversion.PerServing(
            PerServing(Printed(nutrient, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), unit, $"{amount} {unit}")),
            FourServings);

        var value = nutrient switch
        {
            "calories" => values.Calories,
            "protein" => values.ProteinG,
            "sodium" => values.SodiumMg,
            _ => values.FatG,
        };
        _ = await Assert.That(value).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
    }

    [Test]
    [Arguments("sodium")]
    [Arguments("salt")]
    public async Task AnAmountTooLargeToConvert_IsEmptyAndOutOfRange(string nutrient)
    {
        var values = NutritionConversion.PerServing(PerServing(Printed(nutrient, 1e27m, "g", "1e27 g")), FourServings);

        _ = await Assert.That(values.SodiumMg).IsEqualTo(new MarkedValue<int?>(null, OutOfRange));
    }

    [Test]
    [Arguments("calories", "10000", "kcal")]
    [Arguments("protein", "1000", "g")]
    [Arguments("sodium", "50000", "mg")]
    public async Task AResultAtItsLimit_IsKept(string nutrient, string amount, string unit)
    {
        var values = NutritionConversion.PerServing(
            PerServing(Printed(nutrient, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), unit, $"{amount} {unit}")),
            FourServings);

        var value = nutrient switch
        {
            "calories" => values.Calories,
            "protein" => values.ProteinG,
            _ => values.SodiumMg,
        };
        _ = await Assert.That(value).IsEqualTo(Unmarked(int.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task RowsWithNoNutrientName_AreSkipped_AndTheRestConvert()
    {
        var values = NutritionConversion.PerServing(
            PerServing(
                Printed(null, 5m, "g", "5 g"),
                Printed("", 6m, "g", "6 g"),
                Printed(" ", 7m, "g", "7 g"),
                Printed("protein", 12m, "g", "12 g")),
            FourServings);

        _ = await Assert.That(values).IsEqualTo(Empty() with { ProteinG = Unmarked(12) });
    }

    [Test]
    public async Task AValueInAUnitItsNutrientCannotTake_IsLeftEmpty()
    {
        var values = NutritionConversion.PerServing(PerServing(Printed("protein", 12m, "kcal", "12 kcal")), FourServings);

        _ = await Assert.That(values.ProteinG).IsEqualTo(NoValue);
    }

    private static ReaderNutrition PerServing(params ReaderNutrient[] values) => new("serving", values);

    private static ReaderNutrient Printed(string nutrient, decimal amount, string unit, string printed) =>
        new(nutrient, amount, unit, printed, Inexact: false);

    private static MarkedValue<int?> Unmarked(int value) => new(value, null);

    private static MarkedValue<int?> Calculated(int value, string printed) =>
        new(value, new Mark(MarkKinds.Calculated, null, MarkCodes.PrintedAs, printed));

    private static NutritionValues Empty() =>
        new(NoValue, NoValue, NoValue, NoValue, NoValue, NoValue, NoValue, NoValue);
}
