using System.Net.Mime;
using System.Text.Json;
using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public sealed class EvalRunner(
    IEvalService service,
    TextWriter output,
    Func<string, bool> confirm,
    RecipeCatalog catalog = null)
{
    public async Task<int> CheckAsync(IReadOnlyList<string> models, CancellationToken cancellationToken)
    {
        output.WriteLine("Counting the prompt and the output schema, with no photo, for each model. count_tokens is free.");
        var refused = 0;
        foreach (var model in models)
        {
            var count = await service.CountTokensAsync(model, [], null, cancellationToken);
            if (count.InputTokens is { } tokens)
            {
                output.WriteLine($"  {model}: {Report.Tokens(tokens)} input tokens; the endpoint took the output schema.");
            }
            else
            {
                refused++;
                output.WriteLine($"  {model}: the endpoint refused the request. {count.Error}");
            }
        }

        output.WriteLine("A check is a call with the output schema: start a comparison at least 24 hours after it (A18).");
        return refused == 0 ? 0 : 1;
    }

    public async Task<int> DraftAsync(
        IReadOnlyList<EvalCase> cases,
        string model,
        int edge,
        CancellationToken cancellationToken)
    {
        var undrafted = cases
            .Where(evalCase => !File.Exists(evalCase.ExpectedPath) && !File.Exists(evalCase.DraftPath))
            .ToList();
        if (undrafted.Count == 0)
        {
            output.WriteLine("Every case already has an expected.json or an expected.draft.json, so nothing needs a draft.");
            return 0;
        }

        var profile = ModelProfiles.Find(model);
        var modelEdge = ModelEdge(profile, edge);
        if (Encoded(undrafted, modelEdge) is not { } photos)
        {
            return 1;
        }

        output.WriteLine(
            $"Drafting {undrafted.Count} of {cases.Count} cases with {model}, photos at {modelEdge} px. "
            + "A case that already has an expected.json or an expected.draft.json is left out.");
        if (!Confirmed(undrafted.Select(evalCase => (Profile: profile, Photos: photos[evalCase].Count))))
        {
            output.WriteLine("Nothing was read.");
            return 1;
        }

        long spent = 0;
        foreach (var evalCase in undrafted)
        {
            var timed = await service.ReadAsync(model, photos[evalCase], catalog, cancellationToken);
            spent += Cost(profile, timed);
            if (timed.Attempt.Failure is not null)
            {
                output.WriteLine($"  {evalCase.Name}: {Failed(timed)}");
                if (evalCase == undrafted[0] && timed.Attempt.Failure == ReadFailure.ServiceError)
                {
                    output.WriteLine("The first read was a service error, so the draft stops. Report it to the owner.");
                    return 1;
                }

                continue;
            }

            var read = Normalized(timed.Attempt.Output);
            ExpectedAnswer.FromRead(read, evalCase.IsHandwritten).Save(evalCase.DraftPath);
            output.WriteLine(read.Outcome == ReadOutcomes.NoRecipe
                ? $"  {evalCase.Name}: the reader found no recipe; wrote an empty expected.draft.json to fill in by hand."
                : $"  {evalCase.Name}: wrote expected.draft.json ({Report.Seconds(timed.Elapsed)}).");
        }

        output.WriteLine($"Spent about {Report.Usd(spent)}.");
        output.WriteLine(
            "Correct each expected.draft.json against its photos: set handwritten, set stated to false on every value "
            + "the photos do not give, remove every row they do not show, set match to the name of the catalog.json "
            + "recipe that the photos are a version of, or null, and save it as expected.json.");
        return 0;
    }

    public async Task<Comparison> CompareAsync(
        IReadOnlyList<EvalCase> cases,
        IReadOnlyList<string> models,
        int edge,
        CancellationToken cancellationToken)
    {
        if (ExpectedAnswers(cases) is not { } expected)
        {
            return null;
        }

        var edges = models.ToDictionary(model => model, model => ModelEdge(ModelProfiles.Find(model), edge));
        var photos = new Dictionary<int, Dictionary<EvalCase, IReadOnlyList<ReadPhoto>>>();
        foreach (var modelEdge in edges.Values.Distinct())
        {
            if (Encoded(cases, modelEdge) is not { } encoded)
            {
                return null;
            }

            photos[modelEdge] = encoded;
        }

        var cold = cases.FirstOrDefault(evalCase => evalCase.Photos.Count <= ModelTiming.MostPhotosTimed) ?? cases[0];
        List<EvalCase> order = [cold, cold, .. cases.Where(evalCase => evalCase != cold)];
        output.WriteLine(
            $"Comparing {models.Count} models on {cases.Count} cases. Each model reads {cold.Name} first, then reads it "
            + "again, then every other case once, and counts its image tokens only after its reads (A23).");
        output.WriteLine(
            $"Each read carries the catalog of {catalog.Entries.Count} recipes in catalog.json, as Create Recipe does, "
            + "and SC-005 is scored.");
        output.WriteLine(
            "Nothing may have called the API with the output schema in the 24 hours before this run: no --check, "
            + "draft run, other run or real read (A17, A18).");
        var planned = models.SelectMany(model =>
            order.Select(evalCase => (Profile: ModelProfiles.Find(model), Photos: evalCase.Photos.Count)));
        if (!Confirmed(planned))
        {
            output.WriteLine("Nothing was read.");
            return null;
        }

        var results = new List<ModelResult>();
        long spent = 0;
        foreach (var model in models)
        {
            var (result, cost) = await ComparedAsync(
                model,
                edges[model],
                cases,
                order,
                photos[edges[model]],
                expected,
                results.Count == 0,
                cancellationToken);
            spent += cost;
            if (result is null)
            {
                output.WriteLine($"Spent about {Report.Usd(spent)}.");
                return null;
            }

            results.Add(result);
        }

        var comparison = new Comparison(edge, results);
        Report.WriteComparison(comparison, output);
        output.WriteLine($"Spent about {Report.Usd(spent)}.");
        return comparison;
    }

    private static int ModelEdge(ModelProfile profile, int edge) => Math.Min(edge, profile.MaxImageEdge);

    private static long Cost(ModelProfile profile, TimedAttempt timed) =>
        timed.Attempt.Usage is { } usage ? ReadCost.MicroUsd(profile, usage) : 0;

    private static string Failed(TimedAttempt timed) =>
        $"filled no review: {timed.Attempt.Failure} after {Report.Seconds(timed.Elapsed)}"
        + (timed.ServiceError is { } error ? $" ({error})" : string.Empty);

    private static TimedRead Timed(EvalCase evalCase, TimedAttempt timed) =>
        new(evalCase.Name, evalCase.Photos.Count, timed.Elapsed, timed.Attempt.Failure is null);

    private static string Called(int read) => read switch
    {
        0 => " (first call)",
        1 => " (read again)",
        _ => string.Empty,
    };

    private async Task<(ModelResult Result, long Spent)> ComparedAsync(
        string model,
        int edge,
        IReadOnlyList<EvalCase> cases,
        IReadOnlyList<EvalCase> order,
        Dictionary<EvalCase, IReadOnlyList<ReadPhoto>> photos,
        Dictionary<EvalCase, ExpectedAnswer> expected,
        bool firstModel,
        CancellationToken cancellationToken)
    {
        var profile = ModelProfiles.Find(model);
        output.WriteLine($"{model}, photos at {edge} px");
        var reads = new List<(EvalCase Case, TimedAttempt Timed)>();
        long spent = 0;
        foreach (var evalCase in order)
        {
            var timed = await service.ReadAsync(model, photos[evalCase], catalog, cancellationToken);
            spent += Cost(profile, timed);
            output.WriteLine(
                $"  {evalCase.Name}{Called(reads.Count)}: "
                + (timed.Attempt.Failure is null ? Report.Seconds(timed.Elapsed) : Failed(timed)));
            if (firstModel && reads.Count == 0 && timed.Attempt.Failure == ReadFailure.ServiceError)
            {
                output.WriteLine("The run's first read was a service error, so the run stops. Report it to the owner.");
                return (null, spent);
            }

            reads.Add((Case: evalCase, Timed: timed));
        }

        var timing = new ModelTiming(
            Timed(reads[0].Case, reads[0].Timed),
            Timed(reads[1].Case, reads[1].Timed),
            [.. reads.Skip(2).Select(read => Timed(read.Case, read.Timed))]);
        var scores = cases
            .Select(evalCase => Scored(evalCase, expected[evalCase], reads.First(read => read.Case == evalCase).Timed))
            .ToList();
        var costs = reads
            .Where(read => read.Timed.Attempt.Usage is not null)
            .Select(read => ReadCost.MicroUsd(profile, read.Timed.Attempt.Usage))
            .ToList();
        var imageTokens = await ImageTokensAsync(model, cases, photos, cancellationToken);
        long? meanCost = costs.Count == 0 ? null : costs.Sum() / costs.Count;
        return (new ModelResult(model, edge, scores, timing, meanCost, imageTokens), spent);
    }

    private async Task<ImageTokenCount> ImageTokensAsync(
        string model,
        IEnumerable<EvalCase> cases,
        Dictionary<EvalCase, IReadOnlyList<ReadPhoto>> photos,
        CancellationToken cancellationToken)
    {
        var prompt = await service.CountTokensAsync(model, [], null, cancellationToken);
        if (prompt.InputTokens is not { } promptTokens)
        {
            return new ImageTokenCount(null, [], prompt.Error);
        }

        var perPhoto = new List<long>();
        foreach (var photo in cases.SelectMany(evalCase => photos[evalCase]))
        {
            var count = await service.CountTokensAsync(model, [photo], null, cancellationToken);
            if (count.InputTokens is not { } tokens)
            {
                return new ImageTokenCount(promptTokens, perPhoto, count.Error);
            }

            perPhoto.Add(tokens - promptTokens);
        }

        output.WriteLine($"  counted the image tokens of {perPhoto.Count} photos");
        var withCatalog = await service.CountTokensAsync(model, [], catalog, cancellationToken);
        var catalogCount = new CatalogTokenCount(catalog.Characters, withCatalog.InputTokens - promptTokens, withCatalog.Error);
        return new ImageTokenCount(promptTokens, perPhoto, null, catalogCount);
    }

    private CaseScore Scored(EvalCase evalCase, ExpectedAnswer expected, TimedAttempt timed) => CaseScorer.Score(
        evalCase.Name,
        expected,
        timed.Attempt.Failure is null ? Normalized(timed.Attempt.Output) : null);

    private ReadResponse Normalized(ReaderOutput output)
    {
        var read = ReadResultNormalizer.Normalize(output);
        return read.Outcome == ReadOutcomes.Recipe ? read with { Match = catalog?.Match(output.MatchId, output.MatchReason) } : read;
    }

    private bool Confirmed(IEnumerable<(ModelProfile Profile, int Photos)> reads)
    {
        var planned = reads.ToList();
        var estimate = planned.Sum(read => ReadCost.ReservationMicroUsd(read.Profile, read.Photos, catalog?.Characters ?? 0));
        return confirm(
            $"This run makes {planned.Count} paid reads, estimated at up to {Report.Usd(estimate)}: each read is priced as "
            + $"the site reserves it, at the model's highest prices with all {Report.Tokens(ReadCost.MaxTokens)} output "
            + "tokens. Go ahead?");
    }

    private bool InCatalog(string recipeName) =>
        catalog?.Entries.Any(entry => string.Equals(entry.Name, recipeName, StringComparison.OrdinalIgnoreCase)) == true;

    private Dictionary<EvalCase, IReadOnlyList<ReadPhoto>> Encoded(IEnumerable<EvalCase> cases, int edge)
    {
        output.WriteLine($"Encoding the photos as the page would, at {edge} px.");
        var encoded = new Dictionary<EvalCase, IReadOnlyList<ReadPhoto>>();
        var refusals = new List<string>();
        foreach (var evalCase in cases)
        {
            var photos = new List<ReadPhoto>();
            foreach (var path in evalCase.Photos)
            {
                var encoding = PhotoEncoder.Encode(File.ReadAllBytes(path), edge);
                if (encoding.Photo is { } photo)
                {
                    photos.Add(new ReadPhoto(MediaTypeNames.Image.Jpeg, photo.Data));
                }
                else
                {
                    refusals.Add($"  {evalCase.Name}/photos/{Path.GetFileName(path)} {encoding.Refusal}");
                }
            }

            encoded[evalCase] = photos;
        }

        if (refusals.Count == 0)
        {
            return encoded;
        }

        output.WriteLine("Nothing was read, because these photos cannot be sent as the page would send them:");
        refusals.ForEach(output.WriteLine);
        return null;
    }

    private Dictionary<EvalCase, ExpectedAnswer> ExpectedAnswers(IReadOnlyList<EvalCase> cases)
    {
        var answers = new Dictionary<EvalCase, ExpectedAnswer>();
        var problems = new List<string>();
        foreach (var evalCase in cases)
        {
            if (!File.Exists(evalCase.ExpectedPath))
            {
                problems.Add(
                    $"  {evalCase.Name} has no expected.json: run --draft, then correct its expected.draft.json "
                    + "and save it as expected.json.");
                continue;
            }

            try
            {
                answers[evalCase] = ExpectedAnswer.Load(evalCase.ExpectedPath);
            }
            catch (JsonException exception)
            {
                problems.Add($"  {evalCase.Name}: its expected.json cannot be read. {exception.Message}");
                continue;
            }

            if (answers[evalCase].Match is { } match && !InCatalog(match))
            {
                problems.Add($"  {evalCase.Name}: its expected match, {match}, names no recipe in catalog.json.");
            }
        }

        if (answers.Count == cases.Count)
        {
            problems.AddRange(UnjudgedCriteria(cases, answers));
        }

        if (problems.Count == 0)
        {
            return answers;
        }

        output.WriteLine(
            "Nothing was read, because the comparison needs an expected answer for every case and a set that can "
            + "judge SC-002 to SC-005:");
        problems.ForEach(output.WriteLine);
        return null;
    }

    private IEnumerable<string> UnjudgedCriteria(IReadOnlyList<EvalCase> cases, Dictionary<EvalCase, ExpectedAnswer> answers)
    {
        if (catalog is null)
        {
            yield return "  catalog.json is missing, so SC-005 cannot be judged.";
        }

        if (answers.Values.All(answer => answer.Match is null))
        {
            yield return "  no case expects a match, so the on-site half of SC-005 cannot be judged.";
        }

        if (answers.Values.All(answer => answer.Match is not null))
        {
            yield return "  every case expects a match, so the off-site half of SC-005 cannot be judged.";
        }

        if (ExpectedLines(answers, handwritten: true) == 0)
        {
            yield return "  no handwritten case expects an ingredient, so SC-003 cannot be judged.";
        }

        if (ExpectedLines(answers, handwritten: false) == 0)
        {
            yield return "  no printed case expects an ingredient, so SC-003 cannot be judged.";
        }

        if (cases.All(evalCase => evalCase.Photos.Count > ModelTiming.MostPhotosTimed))
        {
            yield return
                $"  every case has more than {ModelTiming.MostPhotosTimed} photos, so SC-002 cannot be judged.";
        }
    }

    private static int ExpectedLines(Dictionary<EvalCase, ExpectedAnswer> answers, bool handwritten) =>
        answers.Values.Where(answer => answer.Handwritten == handwritten).Sum(answer => answer.Ingredients.Count);
}
