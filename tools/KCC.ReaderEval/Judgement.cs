namespace KCC.ReaderEval;

public static class Judgement
{
    private const int PrintedPercent = 95;
    private const int HandwrittenPercent = 85;
    private const int RecipesInOrderOf10 = 9;
    private const int RightMatchesOf10 = 9;
    private const int MatchesOffTheSiteOf10 = 1;

    public static TimeSpan TimeLimit { get; } = TimeSpan.FromSeconds(50);

    public static TimeSpan? LargestColdPenalty(IEnumerable<ModelTiming> timings)
    {
        var penalties = timings.Select(timing => timing.ColdPenalty).ToList();
        return penalties.Count == 0 || penalties.Any(penalty => penalty is null) ? null : penalties.Max();
    }

    public static Verdict Sc002(ModelTiming timing, TimeSpan? largestColdPenalty) =>
        largestColdPenalty is not { } penalty || timing.WarmNinetiethPercentile is not { } warm ? Verdict.NotJudged
        : warm <= TimeLimit - penalty ? Verdict.Met
        : Verdict.NotMet;

    public static Verdict Sc003(IReadOnlyList<CaseScore> cases)
    {
        var printed = Lines(cases, handwritten: false);
        var handwritten = Lines(cases, handwritten: true);
        if (printed.Expected == 0 || handwritten.Expected == 0)
        {
            return Verdict.NotJudged;
        }

        var inOrder = cases.Count(score => score.StepsInOrder);
        return (printed.Right * 100) >= (printed.Expected * PrintedPercent)
            && (handwritten.Right * 100) >= (handwritten.Expected * HandwrittenPercent)
            && (inOrder * 10) >= (cases.Count * RecipesInOrderOf10)
            ? Verdict.Met
            : Verdict.NotMet;
    }

    public static Verdict Sc004(IReadOnlyList<CaseScore> cases) =>
        cases.Count == 0 ? Verdict.NotJudged
        : cases.All(score => score.UnmarkedInventedValues.Count == 0) ? Verdict.Met
        : Verdict.NotMet;

    public static Verdict Sc005(IReadOnlyList<CaseScore> cases)
    {
        var onSite = cases.Where(score => score.Match is { OnSite: true }).ToList();
        var offSite = cases.Where(score => score.Match is { OnSite: false }).ToList();
        if (onSite.Count == 0 || offSite.Count == 0)
        {
            return Verdict.NotJudged;
        }

        return (onSite.Count(score => score.Match.IsRight) * 10) >= (onSite.Count * RightMatchesOf10)
            && (offSite.Count(score => !score.Match.IsRight) * 10) <= (offSite.Count * MatchesOffTheSiteOf10)
            ? Verdict.Met
            : Verdict.NotMet;
    }

    public static (int Right, int Cases) Matches(IReadOnlyList<CaseScore> cases, bool onSite)
    {
        var scores = cases.Where(score => score.Match is { } match && match.OnSite == onSite).ToList();
        return (scores.Count(score => score.Match.IsRight), scores.Count);
    }

    public static (int Right, int Expected) Lines(IReadOnlyList<CaseScore> cases, bool handwritten)
    {
        var scores = cases.Where(score => score.Handwritten == handwritten).ToList();
        return (scores.Sum(score => score.RightLines), scores.Sum(score => score.ExpectedLines));
    }
}

public sealed record TimedRead(string Case, int Photos, TimeSpan Elapsed, bool Filled);

public sealed record ModelTiming(TimedRead Cold, TimedRead Again, IReadOnlyList<TimedRead> Others)
{
    public const int MostPhotosTimed = 3;

    public TimeSpan? ColdPenalty => Cold.Filled && Again.Filled
        ? Cold.Elapsed > Again.Elapsed ? Cold.Elapsed - Again.Elapsed : TimeSpan.Zero
        : null;

    public IReadOnlyList<TimedRead> WarmReads => [.. Others.Prepend(Again).Where(read => read.Photos <= MostPhotosTimed)];

    public TimeSpan? WarmNinetiethPercentile
    {
        get
        {
            var times = WarmReads.Select(read => read.Filled ? read.Elapsed : TimeSpan.MaxValue).Order().ToList();
            return times.Count == 0 ? null : times[(((9 * times.Count) + 9) / 10) - 1];
        }
    }
}

public sealed record ImageTokenCount(
    long? PromptTokens,
    IReadOnlyList<long> PerPhoto,
    string Error,
    CatalogTokenCount Catalog = null);

public sealed record CatalogTokenCount(int Characters, long? Tokens, string Error)
{
    public double? CharactersPerToken => Tokens is > 0 and { } tokens ? (double)Characters / tokens : null;
}

public sealed record ModelResult(
    string Model,
    int Edge,
    IReadOnlyList<CaseScore> Cases,
    ModelTiming Timing,
    long? MeanCostMicroUsd,
    ImageTokenCount ImageTokens);

public sealed record Comparison(int Edge, IReadOnlyList<ModelResult> Models)
{
    public TimeSpan? LargestColdPenalty => Judgement.LargestColdPenalty(Models.Select(model => model.Timing));

    public ModelResult Cheapest => Models
        .Where(model => model.MeanCostMicroUsd is not null && MeetsAll(model))
        .MinBy(model => model.MeanCostMicroUsd);

    public Verdict Sc002(ModelResult model) => Judgement.Sc002(model.Timing, LargestColdPenalty);

    public Verdict Sc003(ModelResult model) => Judgement.Sc003(model.Cases);

    public Verdict Sc004(ModelResult model) => Judgement.Sc004(model.Cases);

    public Verdict Sc005(ModelResult model) => Judgement.Sc005(model.Cases);

    private bool MeetsAll(ModelResult model) =>
        Sc002(model) == Verdict.Met
        && Sc003(model) == Verdict.Met
        && Sc004(model) == Verdict.Met
        && Sc005(model) == Verdict.Met;
}
