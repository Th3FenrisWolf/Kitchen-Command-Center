using System.Globalization;
using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public static class Report
{
    private const decimal MicroUsdPerUsd = 1_000_000m;

    public static string Usd(long microUsd) =>
        string.Create(CultureInfo.InvariantCulture, $"US${microUsd / MicroUsdPerUsd:0.00}");

    public static string UsdPerRead(long microUsd) =>
        string.Create(CultureInfo.InvariantCulture, $"US${microUsd / MicroUsdPerUsd:0.0000}");

    public static string Seconds(TimeSpan time) => time == TimeSpan.MaxValue
        ? "unreached (more than 1 read in 10 filled no review)"
        : string.Create(CultureInfo.InvariantCulture, $"{time.TotalSeconds:0.0} s");

    public static string Tokens(long tokens) => tokens.ToString("N0", CultureInfo.InvariantCulture);

    public static void WriteComparison(Comparison comparison, TextWriter output)
    {
        foreach (var model in comparison.Models)
        {
            WriteModel(comparison, model, output);
        }

        output.WriteLine();
        output.WriteLine(comparison.LargestColdPenalty is { } largest
            ? $"The run's largest cold penalty, which SC-002 adds for every model (A18): {Seconds(largest)}"
            : "A cold penalty was not measured, so no model is judged on SC-002: run the comparison again after 24 hours.");
        output.WriteLine(comparison.Cheapest is { } cheapest
            ? $"Cheapest model that meets SC-002 to SC-005: {cheapest.Model}, "
                + $"{UsdPerRead(cheapest.MeanCostMicroUsd ?? 0)} a read."
            : "No model meets SC-002 to SC-005.");
    }

    private static void WriteModel(Comparison comparison, ModelResult model, TextWriter output)
    {
        var timing = model.Timing;
        var cost = model.MeanCostMicroUsd is { } mean ? UsdPerRead(mean) : "unknown";
        output.WriteLine();
        output.WriteLine($"{model.Model}, photos at {model.Edge} px");
        output.WriteLine(
            $"  Ingredient lines right: printed {Share(model.Cases, false)}, handwritten {Share(model.Cases, true)}");
        output.WriteLine(
            $"  Recipes with every step in order: {model.Cases.Count(score => score.StepsInOrder)} of {model.Cases.Count}");
        output.WriteLine($"  Unmarked invented values: {model.Cases.Sum(score => score.UnmarkedInventedValues.Count)}");
        output.WriteLine($"  {Matching(model.Cases)}");
        output.WriteLine(
            $"  First call {Seconds(timing.Cold)}, the same case again {Seconds(timing.Again)}, "
            + $"cold penalty {Seconds(timing.ColdPenalty)}");
        output.WriteLine(
            $"  Warm 90th percentile {Seconds(timing.WarmNinetiethPercentile)}, over {timing.WarmReads.Count} reads "
            + $"of up to {ModelTiming.MostPhotosTimed} photos");
        output.WriteLine($"  Mean cost {cost} a read");
        output.WriteLine($"  {ImageTokens(model)}");
        if (model.ImageTokens.Catalog is { } catalog)
        {
            output.WriteLine($"  {CatalogTokens(catalog)}");
        }

        output.WriteLine(
            $"  SC-002 {Text(comparison.Sc002(model))} (warm 90th percentile plus the run's largest cold penalty, "
            + $"at most {Seconds(Judgement.TimeLimit)}), SC-003 {Text(comparison.Sc003(model))}, "
            + $"SC-004 {Text(comparison.Sc004(model))}, SC-005 {Text(comparison.Sc005(model))}");
        foreach (var score in model.Cases)
        {
            WriteMisses(score, output);
        }
    }

    private static void WriteMisses(CaseScore score, TextWriter output)
    {
        if (score.MissedLines.Count > 0)
        {
            output.WriteLine($"    {score.Case}: lines not right: {string.Join("; ", score.MissedLines.Select(Line))}");
        }

        if (!score.StepsInOrder)
        {
            output.WriteLine($"    {score.Case}: not every step appears in order");
        }

        if (score.Match is { IsRight: false } match)
        {
            output.WriteLine(
                $"    {score.Case}: proposed {match.ProposedRecipe ?? "no recipe"}, "
                + $"where the photos are {(match.OnSite ? $"a version of {match.ExpectedRecipe}" : "on no site recipe")}");
        }

        if (score.UnmarkedInventedValues.Count > 0)
        {
            output.WriteLine(
                $"    {score.Case}: unmarked invented values: {string.Join("; ", score.UnmarkedInventedValues)}");
        }
    }

    private static string Share(IReadOnlyList<CaseScore> cases, bool handwritten)
    {
        var (right, expected) = Judgement.Lines(cases, handwritten);
        return expected == 0
            ? "no lines"
            : string.Create(CultureInfo.InvariantCulture, $"{100m * right / expected:0.0}% ({right} of {expected})");
    }

    private static string Matching(IReadOnlyList<CaseScore> cases)
    {
        var (rightOnSite, onSite) = Judgement.Matches(cases, onSite: true);
        var (rightOffSite, offSite) = Judgement.Matches(cases, onSite: false);
        return $"Matching: the right recipe proposed in {rightOnSite} of {onSite} cases on the site, "
            + $"a recipe proposed in {offSite - rightOffSite} of {offSite} cases off the site";
    }

    private static string ImageTokens(ModelResult model)
    {
        var tokens = model.ImageTokens;
        if (tokens.Error is { } error)
        {
            return $"Image tokens not counted: {error}";
        }

        var cap = Tokens(ModelProfiles.Find(model.Model).ImageTokenCap);
        return tokens.PerPhoto.Count == 0
            ? $"Image tokens: prompt and schema {Tokens(tokens.PromptTokens ?? 0)}; no photo counted"
            : $"Image tokens: prompt and schema {Tokens(tokens.PromptTokens ?? 0)}; a photo "
                + $"{Tokens((long)Math.Round(tokens.PerPhoto.Average()))} on average, "
                + $"{Tokens(tokens.PerPhoto.Max())} at most "
                + $"(the profile's cap is {cap})";
    }

    private static string CatalogTokens(CatalogTokenCount catalog) => catalog.CharactersPerToken is { } perToken
        ? string.Create(
            CultureInfo.InvariantCulture,
            $"Catalog: {Tokens(catalog.Characters)} characters in {Tokens(catalog.Tokens ?? 0)} tokens, {perToken:0.00} characters a token")
        : $"Catalog tokens not counted: {catalog.Error ?? "the count gave the catalog no tokens"}";

    private static string Seconds(TimedRead read) => read.Filled ? Seconds(read.Elapsed) : "no review";

    private static string Seconds(TimeSpan? time) => time is { } measured ? Seconds(measured) : "not measured";

    private static string Line(ExpectedIngredient line) => string.Join(
        ' ',
        new[] { line.Quantity?.ToString(CultureInfo.InvariantCulture), line.Unit, line.Name }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string Text(Verdict verdict) => verdict switch
    {
        Verdict.Met => "met",
        Verdict.NotMet => "not met",
        _ => "not judged",
    };
}
