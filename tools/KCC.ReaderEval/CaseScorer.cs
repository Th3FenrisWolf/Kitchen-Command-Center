using System.Text.RegularExpressions;
using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public static partial class CaseScorer
{
    private const decimal QuantityTolerance = 0.005m;
    private const double FoundShare = 0.8;
    private const double StatedShare = 0.5;
    private const int SignificantWordLength = 3;

    public static CaseScore Score(string caseName, ExpectedAnswer expected, ReadResponse read)
    {
        var rows = read?.Ingredients ?? [];
        var matches = Matches(expected.Ingredients, rows);
        var expectedSteps = expected.Steps.Select(Words).ToList();
        var stepTexts = Enumerable.Range(1, expected.Steps.Count)
            .Select(step => expected.StepTexts(step).Select(Words).ToList())
            .ToList();
        var readSteps = (read?.Instructions ?? []).Select(step => Words(step.Text)).ToList();
        return new CaseScore(
            caseName,
            expected.Handwritten,
            expected.Ingredients.Count,
            [.. expected.Ingredients.Where((_, line) => matches[line] < 0)],
            InOrder(stepTexts, readSteps),
            read is null ? [] : InventedValues(expected, read, matches, expectedSteps, [.. stepTexts.SelectMany(texts => texts.Skip(1))]),
            new MatchScore(expected.Match, read?.Match?.RecipeName));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Word();

    private static int[] Matches(IReadOnlyList<ExpectedIngredient> expected, IReadOnlyList<IngredientValue> rows)
    {
        var used = new bool[rows.Count];
        var matches = new int[expected.Count];
        for (var line = 0; line < expected.Count; line++)
        {
            matches[line] = -1;
            for (var row = 0; row < rows.Count && matches[line] < 0; row++)
            {
                if (!used[row] && Same(expected[line], rows[row]))
                {
                    used[row] = true;
                    matches[line] = row;
                }
            }
        }

        return matches;
    }

    private static bool Same(ExpectedIngredient line, IngredientValue row) =>
        line.Names.Any(name => string.Equals(Collapsed(name), Collapsed(row.Name), StringComparison.OrdinalIgnoreCase))
        && SameUnit(Collapsed(line.Unit), Collapsed(row.Unit))
        && SameQuantity(line.Quantity, row.Quantity);

    // On many cards T is a tablespoon and t a teaspoon.
    private static bool SameUnit(string expected, string read) => string.Equals(
        expected,
        read,
        expected.Length == 1 || read.Length == 1 ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

    // A third is as right written 0.33 as 0.333.
    private static bool SameQuantity(decimal? expected, decimal? read) =>
        expected is { } wanted && read is { } got
            ? Math.Abs(wanted - got) <= QuantityTolerance
            : expected is null && read is null;

    private static bool InOrder(IReadOnlyList<List<string[]>> expected, IReadOnlyList<string[]> read)
    {
        var from = 0;
        foreach (var texts in expected)
        {
            from = texts.Select(text => EndOfMatch(text, read, from)).Where(end => end >= 0).DefaultIfEmpty(-1).Min();
            if (from < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int EndOfMatch(string[] step, IReadOnlyList<string[]> read, int from)
    {
        for (var end = from; end < read.Count; end++)
        {
            if (Covers(read, end, step, FoundShare, earliestStart: from))
            {
                return end;
            }
        }

        return -1;
    }

    // A step read as two, or two steps read as one, still holds its words in order.
    private static bool Covers(IReadOnlyList<string[]> steps, int end, string[] words, double share, int earliestStart) =>
        Share(words, steps[end]) >= share
        || (end > earliestStart && Share(words, [.. steps[end - 1], .. steps[end]]) >= share);

    private static List<string> InventedValues(
        ExpectedAnswer expected,
        ReadResponse read,
        int[] matches,
        IReadOnlyList<string[]> expectedSteps,
        IReadOnlyList<string[]> alternateSteps)
    {
        var invented = new List<string>();
        AddInvented(invented, "recipe name", expected.Recipe.Name, read.Recipe.Name);
        AddInvented(invented, "recipe description", expected.Recipe.Description, read.Recipe.Description);
        AddInvented(invented, "variant name", expected.Variant.Name, read.Variant.Name);
        AddInvented(invented, "variant description", expected.Variant.Description, read.Variant.Description);
        AddInvented(invented, "prep time", expected.Variant.PrepMinutes, read.Variant.PrepTime);
        AddInvented(invented, "cook time", expected.Variant.CookMinutes, read.Variant.CookTime);
        AddInvented(invented, "servings", expected.Variant.Servings, read.Variant.Servings);

        var rightRows = matches.Where(row => row >= 0).ToHashSet();
        invented.AddRange(read.Ingredients
            .Where((row, index) => row.Mark is null && !rightRows.Contains(index))
            .Select(row => InventedPart(row, expected.Ingredients))
            .OfType<string>());
        invented.AddRange(read.Instructions
            .Where(step => step.Mark is null
                && !IsStated(Words(step.Text), expectedSteps)
                && !IsStated(Words(step.Text), alternateSteps))
            .Select(step => $"step \"{step.Text}\""));
        return invented;
    }

    private static void AddInvented<T>(List<string> invented, string label, ExpectedValue<T> expected, MarkedValue<T> read)
    {
        var filled = read.Value is string text ? !string.IsNullOrWhiteSpace(text) : read.Value is not null;
        if (!expected.Stated && filled && read.Mark is null)
        {
            invented.Add($"{label} \"{read.Value}\"");
        }
    }

    private static string InventedPart(IngredientValue row, IReadOnlyList<ExpectedIngredient> expected)
    {
        if (Counterpart(row.Name, expected) is not { } line)
        {
            return $"ingredient \"{row.Name}\"";
        }

        var parts = new List<string>();
        if (line.Quantity is null && row.Quantity is { } quantity)
        {
            parts.Add($"quantity {quantity}");
        }

        if (string.IsNullOrWhiteSpace(line.Unit) && !string.IsNullOrWhiteSpace(row.Unit))
        {
            parts.Add($"unit {row.Unit}");
        }

        return parts.Count == 0 ? null : $"ingredient \"{row.Name}\": {string.Join(", ", parts)}";
    }

    private static ExpectedIngredient Counterpart(string name, IReadOnlyList<ExpectedIngredient> expected)
    {
        var words = SignificantWords(name);
        return expected
            .Select(line => (Line: line, Shared: line.Names.Max(spelling => SignificantWords(spelling).Count(words.Contains))))
            .Where(candidate => candidate.Shared > 0)
            .OrderByDescending(candidate => candidate.Shared)
            .Select(candidate => candidate.Line)
            .FirstOrDefault();
    }

    private static bool IsStated(string[] words, IReadOnlyList<string[]> expectedSteps) =>
        Enumerable.Range(0, expectedSteps.Count)
            .Any(end => Covers(expectedSteps, end, words, StatedShare, earliestStart: 0));

    private static double Share(string[] words, string[] within) =>
        words.Length == 0 ? 1 : (double)CommonSubsequence(words, within) / words.Length;

    private static int CommonSubsequence(string[] first, string[] second)
    {
        var previous = new int[second.Length + 1];
        var current = new int[second.Length + 1];
        foreach (var word in first)
        {
            for (var index = 1; index <= second.Length; index++)
            {
                current[index] = word == second[index - 1]
                    ? previous[index - 1] + 1
                    : Math.Max(previous[index], current[index - 1]);
            }

            (previous, current) = (current, previous);
        }

        return previous[second.Length];
    }

    private static string[] Words(string text) =>
        [.. Word().Matches((text ?? string.Empty).ToLowerInvariant()).Select(match => match.Value)];

    private static HashSet<string> SignificantWords(string text) =>
        [.. Words(text).Where(word => word.Length >= SignificantWordLength)];

    private static string Collapsed(string text) =>
        string.Join(' ', (text ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
}

public sealed record CaseScore(
    string Case,
    bool Handwritten,
    int ExpectedLines,
    IReadOnlyList<ExpectedIngredient> MissedLines,
    bool StepsInOrder,
    IReadOnlyList<string> UnmarkedInventedValues,
    MatchScore Match = null)
{
    public int RightLines => ExpectedLines - MissedLines.Count;
}

public sealed record MatchScore(string ExpectedRecipe, string ProposedRecipe)
{
    public bool OnSite => ExpectedRecipe is not null;

    public bool IsRight => OnSite
        ? string.Equals(ExpectedRecipe, ProposedRecipe, StringComparison.OrdinalIgnoreCase)
        : ProposedRecipe is null;
}
