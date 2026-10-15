using KCC.ReaderEval;
using KCC.Web.Features.RecipeImport;

namespace KCC.UnitTests.Tools;

public class ReaderEvalScorerTests
{
    private static readonly Mark Estimate = new(MarkKinds.Estimate, "Not printed", null, null);
    private static readonly Mark Written = new(MarkKinds.Written, "Not in the photos", null, null);
    private static readonly Mark Unclear = new(MarkKinds.Unclear, "Smudged", null, null);

    [Test]
    public async Task TheColdPenalty_IsTheFirstCallsTimeMinusThatCaseReadAgain()
    {
        var timing = Timing(cold: 31, again: 12, others: [10, 11]);

        _ = await Assert.That(timing.ColdPenalty).IsEqualTo(TimeSpan.FromSeconds(19));
    }

    [Test]
    public async Task AFirstCallNoSlowerThanTheReadAgain_HasNoColdPenalty()
    {
        var timing = Timing(cold: 9, again: 12, others: []);

        _ = await Assert.That(timing.ColdPenalty).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    [Arguments(false, true)]
    [Arguments(true, false)]
    public async Task TheColdPenalty_IsNotMeasured_WhenTheFirstCallOrTheReadAgainFilledNoReview(bool coldFilled, bool againFilled)
    {
        var timing = new ModelTiming(Read(31, filled: coldFilled), Read(12, filled: againFilled), [Read(10)]);

        _ = await Assert.That(timing.ColdPenalty).IsNull();
    }

    [Test]
    public async Task TheRunsLargestColdPenalty_AppliesToEveryModel()
    {
        var coldStart = Timing(cold: 40, again: 20, others: [20, 20, 20]);
        var warmStart = Timing(cold: 32, again: 31, others: [31, 31, 31]);

        var largest = Judgement.LargestColdPenalty([coldStart, warmStart]);

        _ = await Assert.That(largest).IsEqualTo(TimeSpan.FromSeconds(20));
        _ = await Assert.That(Judgement.Sc002(coldStart, largest)).IsEqualTo(Verdict.Met);
        _ = await Assert.That(Judgement.Sc002(warmStart, warmStart.ColdPenalty)).IsEqualTo(Verdict.Met);
        _ = await Assert.That(Judgement.Sc002(warmStart, largest)).IsEqualTo(Verdict.NotMet);
    }

    [Test]
    public async Task TheRunsLargestColdPenalty_IsUnknown_WhileAnyModelsColdPenaltyIsUnmeasured()
    {
        var measured = Timing(cold: 40, again: 20, others: [20]);
        var unmeasured = new ModelTiming(Read(31, filled: false), Read(12), [Read(10)]);

        var largest = Judgement.LargestColdPenalty([measured, unmeasured]);

        _ = await Assert.That(largest).IsNull();
        _ = await Assert.That(Judgement.Sc002(measured, largest)).IsEqualTo(Verdict.NotJudged);
    }

    [Test]
    public async Task TheWarm90thPercentile_CoversTheReadAgainAndTheOtherCalls_ButNeverTheFirstCall()
    {
        var timing = Timing(cold: 45, again: 1, others: [7, 2, 9, 3, 10, 4, 8, 5, 6]);

        _ = await Assert.That(timing.WarmNinetiethPercentile).IsEqualTo(TimeSpan.FromSeconds(9));
    }

    [Test]
    public async Task TheWarm90thPercentile_IsTheNearestRank_SoNineOf10ReadsAreAtOrUnderIt()
    {
        var timing = Timing(cold: 5, again: 1, others: [.. Enumerable.Range(2, 19).Select(seconds => (double)seconds)]);

        _ = await Assert.That(timing.WarmNinetiethPercentile).IsEqualTo(TimeSpan.FromSeconds(18));
    }

    [Test]
    public async Task TheWarm90thPercentile_LeavesOutCasesOfMoreThanThreePhotos()
    {
        var timing = new ModelTiming(Read(20), Read(10), [Read(10), Read(10, photos: 3), Read(59, photos: 4)]);

        _ = await Assert.That(timing.WarmNinetiethPercentile).IsEqualTo(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task AWarmReadThatFilledNoReview_CountsAsNeverFilled()
    {
        var oneFailed = new ModelTiming(Read(20), Read(10), [.. Enumerable.Repeat(Read(10), 8), Read(1, filled: false)]);
        var twoFailed = new ModelTiming(
            Read(20),
            Read(10),
            [.. Enumerable.Repeat(Read(10), 7), Read(1, filled: false), Read(1, filled: false)]);

        _ = await Assert.That(oneFailed.WarmNinetiethPercentile).IsEqualTo(TimeSpan.FromSeconds(10));
        _ = await Assert.That(twoFailed.WarmNinetiethPercentile).IsEqualTo(TimeSpan.MaxValue);
        _ = await Assert.That(Judgement.Sc002(twoFailed, TimeSpan.Zero)).IsEqualTo(Verdict.NotMet);
    }

    [Test]
    [Arguments(30.0, 20.0, Verdict.Met)]
    [Arguments(30.001, 20.0, Verdict.NotMet)]
    [Arguments(50.0, 0.0, Verdict.Met)]
    [Arguments(25.0, 25.5, Verdict.NotMet)]
    public async Task Sc002_IsMetOnlyWhenTheWarm90thPercentilePlusTheLargestColdPenaltyIsAtMost50Seconds(
        double warmSeconds,
        double largestPenaltySeconds,
        Verdict verdict)
    {
        var timing = Timing(cold: warmSeconds, again: warmSeconds, others: [warmSeconds]);

        _ = await Assert.That(Judgement.Sc002(timing, TimeSpan.FromSeconds(largestPenaltySeconds))).IsEqualTo(verdict);
    }

    [Test]
    public async Task AnIngredientLine_IsRight_WhenItsNameUnitAndQuantityMatch()
    {
        var expected = Expected(ingredients: [Line("Flour", 1.5m, "cups"), Line("Sugar", 0.333m, "cup"), Line("salt", null, "")]);
        var read = Response(ingredients: [Row("  flour ", 1.5m, "cups"), Row("SUGAR", 0.33m, "cup"), Row("Salt", null, "")]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.ExpectedLines).IsEqualTo(3);
        _ = await Assert.That(score.RightLines).IsEqualTo(3);
        _ = await Assert.That(score.MissedLines).IsEmpty();
    }

    [Test]
    [Arguments("Flour", 2.0, "cups")]
    [Arguments("Flour", 1.5, "cup")]
    [Arguments("Bread flour", 1.5, "cups")]
    [Arguments("Flour", null, "cups")]
    public async Task AnIngredientLine_IsWrong_WhenItsNameUnitOrQuantityDiffers(string name, double? quantity, string unit)
    {
        var expected = Expected(ingredients: [Line("Flour", 1.5m, "cups")]);
        var read = Response(ingredients: [Row(name, (decimal?)quantity, unit)]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.RightLines).IsEqualTo(0);
        _ = await Assert.That(score.MissedLines.Single()).IsEqualTo(Line("Flour", 1.5m, "cups"));
    }

    [Test]
    [Arguments("Mozzarella cheese", 1)]
    [Arguments("mozz  cheese", 1)]
    [Arguments("cheddar cheese", 0)]
    public async Task AnIngredientName_IsRight_WhenItIsTheNameAsWrittenOrASpellingTheAnswerAccepts(string name, int right)
    {
        var expected = Expected(ingredients: [Line("mozz cheese", 2m, "cups", accepted: ["mozzarella cheese"])]);
        var read = Response(ingredients: [Row(name, 2m, "cups")]);

        var score = CaseScorer.Score("handwritten-pizza", expected, read);

        _ = await Assert.That(score.RightLines).IsEqualTo(right);
    }

    [Test]
    public async Task AnIngredientWithNoAcceptedSpelling_IsRightOnlyByTheNameAsWritten()
    {
        var expected = Expected(ingredients: [Line("mozz cheese", 2m, "cups", accepted: [])]);
        var read = Response(ingredients: [Row("mozzarella cheese", 2m, "cups")]);

        var score = CaseScorer.Score("handwritten-pizza", expected, read);

        _ = await Assert.That(score.RightLines).IsEqualTo(0);
    }

    [Test]
    public async Task ARowNamedByAnAcceptedSpelling_IsNoInventedIngredient_EvenWhenItsQuantityIsWrong()
    {
        var expected = Expected(ingredients: [Line("mozz", 2m, "cups", accepted: ["mozzarella"])]);
        var read = Response(ingredients: [Row("Mozzarella", 3m, "cups")]);

        var score = CaseScorer.Score("handwritten-pizza", expected, read);

        _ = await Assert.That(score.RightLines).IsEqualTo(0);
        _ = await Assert.That(score.UnmarkedInventedValues).IsEmpty();
    }

    [Test]
    public async Task ASingleLetterUnit_KeepsItsCase_SoATablespoonIsNoTeaspoon()
    {
        var expected = Expected(ingredients: [Line("butter", 1m, "T"), Line("salt", 1m, "Tbsp")]);
        var read = Response(ingredients: [Row("butter", 1m, "t"), Row("salt", 1m, "tbsp")]);

        var score = CaseScorer.Score("handwritten-cake", expected, read);

        _ = await Assert.That(score.MissedLines).IsEquivalentTo([Line("butter", 1m, "T")]);
    }

    [Test]
    public async Task EachReadLine_IsRightForOneExpectedLineOnly()
    {
        var expected = Expected(ingredients: [Line("egg", 1m, ""), Line("egg", 1m, "")]);
        var read = Response(ingredients: [Row("egg", 1m, "")]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.RightLines).IsEqualTo(1);
    }

    [Test]
    public async Task AReadThatFilledNoReview_GetsNoLineRight_AndNoStepInOrder()
    {
        var expected = Expected(ingredients: [Line("Flour", 1.5m, "cups")], steps: ["Mix."]);

        var score = CaseScorer.Score("printed-cake", expected, null);

        _ = await Assert.That(score.RightLines).IsEqualTo(0);
        _ = await Assert.That(score.StepsInOrder).IsFalse();
        _ = await Assert.That(score.UnmarkedInventedValues).IsEmpty();
    }

    [Test]
    public async Task TheSteps_AppearInOrder_WhenEachIsReadInItsPlace_EvenWithASmallDifference()
    {
        var expected = Expected(steps: ["Preheat the oven to 350°F.", "Whisk the flour, sugar and salt together.", "Bake for 25 minutes."]);
        var read = Response(steps: ["Preheat the oven to 350 °F.", "Whisk the flour, the sugar and salt together.", "Bake for 25 minutes."]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.StepsInOrder).IsTrue();
    }

    [Test]
    [Arguments("Preheat the oven to 350°F.|Bake for 25 minutes.")]
    [Arguments("Preheat the oven to 350°F.|Bake for 25 minutes.|Whisk the flour, sugar and salt together.")]
    [Arguments("Preheat the oven to 350°F.|Stir the batter well.|Bake for 25 minutes.")]
    public async Task TheSteps_DoNotAppearInOrder_WhenOneIsMissingMovedOrMisread(string readSteps)
    {
        var expected = Expected(steps: ["Preheat the oven to 350°F.", "Whisk the flour, sugar and salt together.", "Bake for 25 minutes."]);
        var read = Response(steps: readSteps.Split('|'));

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.StepsInOrder).IsFalse();
    }

    [Test]
    public async Task TwoStepsReadAsOne_AndOneStepReadAsTwo_StillAppearInOrder()
    {
        var expected = Expected(steps:
        [
            "Preheat the oven to 350°F.",
            "Grease a loaf pan.",
            "Whisk the flour, sugar and salt together, then fold in the berries and pour into the pan.",
        ]);
        var read = Response(steps:
        [
            "Preheat the oven to 350°F. Grease a loaf pan.",
            "Whisk the flour, sugar and salt together.",
            "Then fold in the berries and pour into the pan.",
        ]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.StepsInOrder).IsTrue();
    }

    [Test]
    public async Task AStep_AppearsInOrder_WhenItIsReadAsAnAcceptedAlternate()
    {
        var steps = new[] { "Sauté mushrooms in butter (5 min) with salt and pepper.", "Serve hot." };
        var plain = Expected(steps: steps);
        var accepting = plain with
        {
            AcceptedSteps = new Dictionary<int, IReadOnlyList<string>> { [1] = ["Sautee mushrooms in butter (5 min) w/salt + pepper."] },
        };
        var read = Response(steps: ["Sautee mushrooms in butter (5 min) w/salt + pepper.", "Serve hot."]);

        _ = await Assert.That(CaseScorer.Score("handwritten-mushrooms", plain, read).StepsInOrder).IsFalse();
        _ = await Assert.That(CaseScorer.Score("handwritten-mushrooms", accepting, read).StepsInOrder).IsTrue();
        _ = await Assert.That(CaseScorer.Score("handwritten-mushrooms", accepting, read).UnmarkedInventedValues).IsEmpty();
    }

    [Test]
    public async Task Sc003_HoldsPrintedLinesTo95PercentAndHandwrittenLinesTo85Percent()
    {
        var atTheBars = new[] { Scored(handwritten: false, expected: 20, right: 19), Scored(handwritten: true, expected: 20, right: 17) };
        var printedShort = new[] { Scored(handwritten: false, expected: 20, right: 18), Scored(handwritten: true, expected: 20, right: 20) };
        var handwrittenShort = new[] { Scored(handwritten: false, expected: 20, right: 20), Scored(handwritten: true, expected: 20, right: 16) };

        _ = await Assert.That(Judgement.Sc003(atTheBars)).IsEqualTo(Verdict.Met);
        _ = await Assert.That(Judgement.Sc003(printedShort)).IsEqualTo(Verdict.NotMet);
        _ = await Assert.That(Judgement.Sc003(handwrittenShort)).IsEqualTo(Verdict.NotMet);
    }

    [Test]
    public async Task Sc003_NeedsEveryStepInOrder_InAtLeast9Of10Recipes()
    {
        CaseScore[] nineInOrder =
        [
            .. Enumerable.Range(0, 9).Select(index => Scored(handwritten: index % 2 == 0, expected: 5, right: 5)),
            Scored(handwritten: false, expected: 5, right: 5, stepsInOrder: false),
        ];
        CaseScore[] eightInOrder =
        [
            .. nineInOrder[..8],
            Scored(handwritten: true, expected: 5, right: 5, stepsInOrder: false),
            Scored(handwritten: false, expected: 5, right: 5, stepsInOrder: false),
        ];

        _ = await Assert.That(Judgement.Sc003(nineInOrder)).IsEqualTo(Verdict.Met);
        _ = await Assert.That(Judgement.Sc003(eightInOrder)).IsEqualTo(Verdict.NotMet);
    }

    [Test]
    public async Task Sc003_IsNotJudged_WithoutPrintedOrHandwrittenLines()
    {
        _ = await Assert.That(Judgement.Sc003([Scored(handwritten: false, expected: 10, right: 10)])).IsEqualTo(Verdict.NotJudged);
        _ = await Assert.That(Judgement.Sc003([Scored(handwritten: true, expected: 10, right: 10)])).IsEqualTo(Verdict.NotJudged);
    }

    [Test]
    public async Task AFilledValueThatThePhotosDoNotState_IsAnUnmarkedInventedValue_UnlessItCarriesAMark()
    {
        var expected = Expected(prepStated: false, cookStated: true, descriptionStated: false, variantNameStated: false);
        var read = Response(prep: new(10, null), cook: new(15, null), description: new("Fluffy.", Written), variantName: new("Classic", null));

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.UnmarkedInventedValues.Count).IsEqualTo(2);
        _ = await Assert.That(score.UnmarkedInventedValues.Any(value => value.Contains("10", StringComparison.Ordinal))).IsTrue();
        _ = await Assert.That(score.UnmarkedInventedValues.Any(value => value.Contains("Classic", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task AValueThatThePhotosDoNotState_LeftEmpty_IsNoInventedValue()
    {
        var expected = Expected(prepStated: false, descriptionStated: false);
        var read = Response(prep: new(null, null), description: new(string.Empty, null));

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.UnmarkedInventedValues).IsEmpty();
    }

    [Test]
    public async Task AnUnmarkedIngredientOrStepThatThePhotosDoNotShow_IsAnUnmarkedInventedValue()
    {
        var expected = Expected(
            ingredients: [Line("Flour", 2m, "cups"), Line("Eggs", 2m, "")],
            steps: ["Mix the flour and the eggs.", "Bake for 20 minutes."]);
        var read = Response(
            ingredients:
            [
                Row("Flour", 2m, "cups"),
                Row("Eggs", 2m, ""),
                Row("Vanilla extract", 1m, "tsp"),
                Row("Nutmeg", 0.25m, "tsp", Unclear),
            ],
            steps:
            [
                new InstructionValue("Mix the flour and the eggs.", null),
                new InstructionValue("Bake for 20 minutes.", null),
                new InstructionValue("Dust with icing sugar before serving.", null),
                new InstructionValue("Let it cool on a rack.", Written),
            ]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.UnmarkedInventedValues.Count).IsEqualTo(2);
        _ = await Assert.That(score.UnmarkedInventedValues.Any(value => value.Contains("Vanilla extract", StringComparison.Ordinal))).IsTrue();
        _ = await Assert.That(score.UnmarkedInventedValues.Any(value => value.Contains("icing sugar", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task AQuantityThatThePhotosDoNotGive_IsAnUnmarkedInventedValue()
    {
        var expected = Expected(ingredients: [Line("Salt", null, "")]);
        var marked = Response(ingredients: [Row("salt", 1m, "tsp", Estimate)]);
        var unmarked = Response(ingredients: [Row("salt", 1m, "tsp")]);

        _ = await Assert.That(CaseScorer.Score("printed-cake", expected, marked).UnmarkedInventedValues).IsEmpty();
        _ = await Assert.That(CaseScorer.Score("printed-cake", expected, unmarked).UnmarkedInventedValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AStepSplitInTwo_OrALineReadInTwo_IsNoInventedValue()
    {
        var expected = Expected(
            ingredients: [Line("Salt and pepper", null, "")],
            steps: ["Whisk the flour, sugar and salt together, then fold in the berries and pour into the pan."]);
        var read = Response(
            ingredients: [Row("Salt", null, ""), Row("Pepper", null, "")],
            steps:
            [
                new InstructionValue("Whisk the flour, sugar and salt together.", null),
                new InstructionValue("Then fold in the berries and pour into the pan.", null),
            ]);

        var score = CaseScorer.Score("printed-cake", expected, read);

        _ = await Assert.That(score.UnmarkedInventedValues).IsEmpty();
    }

    [Test]
    public async Task Sc004_IsMetOnlyWithNoUnmarkedInventedValue()
    {
        var clean = Scored(handwritten: false, expected: 5, right: 5);
        var invented = clean with { UnmarkedInventedValues = ["prep time 10"] };

        _ = await Assert.That(Judgement.Sc004([clean, clean])).IsEqualTo(Verdict.Met);
        _ = await Assert.That(Judgement.Sc004([clean, invented])).IsEqualTo(Verdict.NotMet);
        _ = await Assert.That(Judgement.Sc004([])).IsEqualTo(Verdict.NotJudged);
    }

    [Test]
    public async Task AnOnSiteCase_IsRight_OnlyWhenTheReadProposesTheExpectedRecipe()
    {
        var expected = Expected() with { Match = "Weeknight Tacos" };

        var right = CaseScorer.Score("printed-tacos", expected, Proposing("weeknight tacos"));
        var another = CaseScorer.Score("printed-tacos", expected, Proposing("Golden Milk"));
        var none = CaseScorer.Score("printed-tacos", expected, Proposing(null));
        var failed = CaseScorer.Score("printed-tacos", expected, null);

        _ = await Assert.That(right.Match).IsEqualTo(new MatchScore("Weeknight Tacos", "weeknight tacos"));
        _ = await Assert.That(right.Match.OnSite).IsTrue();
        _ = await Assert.That(right.Match.IsRight).IsTrue();
        _ = await Assert.That(another.Match.IsRight).IsFalse();
        _ = await Assert.That(none.Match.IsRight).IsFalse();
        _ = await Assert.That(failed.Match).IsEqualTo(new MatchScore("Weeknight Tacos", null));
        _ = await Assert.That(failed.Match.IsRight).IsFalse();
    }

    [Test]
    public async Task AnOffSiteCase_IsRight_OnlyWhenTheReadProposesNoRecipe()
    {
        var expected = Expected();

        var none = CaseScorer.Score("printed-cake", expected, Proposing(null));
        var proposed = CaseScorer.Score("printed-cake", expected, Proposing("Golden Milk"));
        var failed = CaseScorer.Score("printed-cake", expected, null);

        _ = await Assert.That(none.Match.OnSite).IsFalse();
        _ = await Assert.That(none.Match.IsRight).IsTrue();
        _ = await Assert.That(proposed.Match.IsRight).IsFalse();
        _ = await Assert.That(failed.Match.IsRight).IsTrue();
    }

    [Test]
    [Arguments(9, 1, Verdict.Met)]
    [Arguments(10, 0, Verdict.Met)]
    [Arguments(8, 1, Verdict.NotMet)]
    [Arguments(9, 2, Verdict.NotMet)]
    public async Task Sc005_NeedsTheRightRecipeIn9Of10OnSiteCases_AndARecipeInAtMost1Of10OffSiteCases(
        int rightOnSite,
        int proposedOffSite,
        Verdict verdict)
    {
        CaseScore[] cases =
        [
            .. Enumerable.Range(0, 10).Select(index => Matched("Tacos", index < rightOnSite ? "Tacos" : "Toast")),
            .. Enumerable.Range(0, 10).Select(index => Matched(null, index < proposedOffSite ? "Toast" : null)),
        ];

        _ = await Assert.That(Judgement.Sc005(cases)).IsEqualTo(verdict);
    }

    [Test]
    public async Task Sc005_IsNotJudged_WithoutOnSiteOrOffSiteCases_OrWithoutACatalog()
    {
        _ = await Assert.That(Judgement.Sc005([Matched("Tacos", "Tacos")])).IsEqualTo(Verdict.NotJudged);
        _ = await Assert.That(Judgement.Sc005([Matched(null, null)])).IsEqualTo(Verdict.NotJudged);
        _ = await Assert.That(Judgement.Sc005([Scored(handwritten: false, expected: 5, right: 5)])).IsEqualTo(Verdict.NotJudged);
    }

    [Test]
    public async Task TheCheapestModelThatMeetsSc002ToSc005_IsNamed()
    {
        var passing = Result("claude-opus-5-5", meanCostMicroUsd: 61_000);
        var cheaperPassing = Result("claude-sonnet-5-5", meanCostMicroUsd: 2_400);
        var cheapestFailing = Result("claude-haiku-4-5", meanCostMicroUsd: 900, invented: true);
        var slowest = Result("claude-sonnet-5-5", meanCostMicroUsd: 1_000, warmSeconds: 49);
        var mismatching = Result("claude-haiku-5-5", meanCostMicroUsd: 800, mismatched: true);

        var comparison = new Comparison(2000, [passing, cheaperPassing, cheapestFailing, slowest, mismatching]);

        _ = await Assert.That(comparison.LargestColdPenalty).IsEqualTo(TimeSpan.FromSeconds(2));
        _ = await Assert.That(comparison.Sc002(slowest)).IsEqualTo(Verdict.NotMet);
        _ = await Assert.That(comparison.Sc004(cheapestFailing)).IsEqualTo(Verdict.NotMet);
        _ = await Assert.That(comparison.Sc005(mismatching)).IsEqualTo(Verdict.NotMet);
        _ = await Assert.That(comparison.Sc005(passing)).IsEqualTo(Verdict.Met);
        _ = await Assert.That(comparison.Cheapest?.Model).IsEqualTo("claude-sonnet-5-5");
        _ = await Assert.That(comparison.Cheapest?.MeanCostMicroUsd).IsEqualTo(2_400L);
    }

    [Test]
    public async Task NoModelIsNamed_WhenNoneMeetsSc002ToSc005()
    {
        var comparison = new Comparison(
            2000,
            [Result("claude-haiku-4-5", meanCostMicroUsd: 900, invented: true), Result("claude-haiku-5-5", meanCostMicroUsd: 800, mismatched: true)]);

        _ = await Assert.That(comparison.Cheapest).IsNull();
    }

    [Test]
    public async Task TheReport_GivesEachModelsMatchingAndItsSc005Verdict()
    {
        var output = new StringWriter();

        Report.WriteComparison(new Comparison(2000, [Result("claude-haiku-5-5", meanCostMicroUsd: 800, mismatched: true)]), output);

        var report = output.ToString();
        _ = await Assert.That(report.Contains("SC-005 not met", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(report.Contains("0 of 1", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(report.Contains("No model meets SC-002 to SC-005.", StringComparison.Ordinal)).IsTrue();
    }

    private static TimedRead Read(double seconds, int photos = 1, bool filled = true) =>
        new("case", photos, TimeSpan.FromSeconds(seconds), filled);

    private static ModelTiming Timing(double cold, double again, double[] others) =>
        new(Read(cold), Read(again), [.. others.Select(seconds => Read(seconds))]);

    private static CaseScore Scored(bool handwritten, int expected, int right, bool stepsInOrder = true) => new(
        "case",
        handwritten,
        expected,
        [.. Enumerable.Range(0, expected - right).Select(index => Line($"missed {index}", 1m, "cup"))],
        stepsInOrder,
        []);

    private static ModelResult Result(
        string model,
        long meanCostMicroUsd,
        bool invented = false,
        double warmSeconds = 10,
        bool mismatched = false) => new(
        model,
        2000,
        [
            Scored(handwritten: false, expected: 20, right: 20) with
            {
                UnmarkedInventedValues = invented ? ["servings 4"] : [],
                Match = new MatchScore("Pancakes", mismatched ? null : "Pancakes"),
            },
            Scored(handwritten: true, expected: 20, right: 20) with { Match = new MatchScore(null, null) },
        ],
        Timing(cold: warmSeconds + 2, again: warmSeconds, others: [warmSeconds]),
        meanCostMicroUsd,
        new ImageTokenCount(3_000, [4_000], null));

    private static CaseScore Matched(string expectedRecipe, string proposedRecipe) =>
        Scored(handwritten: false, expected: 5, right: 5) with { Match = new MatchScore(expectedRecipe, proposedRecipe) };

    private static ReadResponse Proposing(string recipeName) => Response() with
    {
        Match = recipeName is null ? null : new RecipeMatch(Guid.NewGuid(), recipeName, "Same dish."),
    };

    private static ExpectedIngredient Line(string name, decimal? quantity, string unit, string[] accepted = null) =>
        new(name, quantity, unit, accepted);

    private static IngredientValue Row(string name, decimal? quantity, string unit, Mark mark = null) =>
        new(name, quantity, unit, quantity is null, mark);

    private static ExpectedAnswer Expected(
        ExpectedIngredient[] ingredients = null,
        string[] steps = null,
        bool prepStated = true,
        bool cookStated = true,
        bool descriptionStated = true,
        bool variantNameStated = true) => new(
        Handwritten: false,
        new ExpectedRecipe(new("Cake", true), new("A cake.", descriptionStated)),
        new ExpectedVariant(
            new("Classic", variantNameStated),
            new("The classic.", true),
            new(10, prepStated),
            new(15, cookStated),
            new(4, true)),
        ingredients ?? [],
        steps ?? []);

    private static ReadResponse Response(
        IngredientValue[] ingredients = null,
        string[] steps = null,
        MarkedValue<int?> prep = null,
        MarkedValue<int?> cook = null,
        MarkedValue<string> description = null,
        MarkedValue<string> variantName = null) =>
        Response(ingredients, [.. (steps ?? []).Select(step => new InstructionValue(step, null))], prep, cook, description, variantName);

    private static ReadResponse Response(
        IngredientValue[] ingredients,
        InstructionValue[] steps,
        MarkedValue<int?> prep = null,
        MarkedValue<int?> cook = null,
        MarkedValue<string> description = null,
        MarkedValue<string> variantName = null) => new(
        ReadOutcomes.Recipe,
        new RecipeValues(new("Cake", null), description ?? new("A cake.", null)),
        new VariantValues(
            variantName ?? new("Classic", null),
            new("The classic.", null),
            prep ?? new(10, null),
            cook ?? new(15, null),
            new(4, null)),
        ingredients ?? [],
        steps,
        Match: null,
        PossibleDuplicate: null,
        OtherRecipes: [],
        Recipes: null);
}
