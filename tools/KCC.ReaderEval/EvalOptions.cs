using System.Globalization;
using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public sealed record EvalOptions(EvalMode Mode, IReadOnlyList<string> Models, int Edge)
{
    public const string DraftModel = "claude-opus-5-5";

    public const string Usage =
        """
        Usage: dotnet run --project tools/KCC.ReaderEval -- <run> [--edge <px>]

          --check             Count the prompt and the output schema for each model, with no photo. Free.
          --draft             Read each case that has no expected answer yet with claude-opus-5-5, and write its
                              expected.draft.json. Paid: states its estimate and asks first.
          --models <a,b,...>  Compare these models on the set. Paid: states its estimate and asks first.
                              With --check, counts for these models only.
          --edge <px>         The longest photo edge that the page would send. Default: the MaxImageEdge default.

        A catalog.json in the set gives every read the catalog that Create Recipe sends, and scores SC-005.
        """;

    private const string CheckOption = "--check";
    private const string DraftOption = "--draft";
    private const string ModelsOption = "--models";
    private const string EdgeOption = "--edge";

    private static readonly string[] Options = [CheckOption, DraftOption, ModelsOption, EdgeOption];

    public static IReadOnlyList<string> ComparedModels { get; } =
        ["claude-haiku-5-5", "claude-haiku-4-5", "claude-sonnet-5-5", "claude-opus-5-5"];

    public static bool TryParse(IReadOnlyList<string> args, out EvalOptions options, out string error)
    {
        options = null;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Count; index++)
        {
            var name = args[index];
            if (!Options.Contains(name))
            {
                error = $"Unknown argument: {name}.";
                return false;
            }

            var takesValue = name is ModelsOption or EdgeOption;
            if (takesValue && index + 1 >= args.Count)
            {
                error = $"{name} needs a value.";
                return false;
            }

            if (!values.TryAdd(name, takesValue ? args[++index] : null))
            {
                error = $"{name} is given twice.";
                return false;
            }
        }

        error = Validated(values, out options);
        return error is null;
    }

    private static string Validated(Dictionary<string, string> values, out EvalOptions options)
    {
        options = null;
        var check = values.ContainsKey(CheckOption);
        var draft = values.ContainsKey(DraftOption);
        if (check && draft)
        {
            return "Choose --check or --draft, not both.";
        }

        if (!check && !draft && !values.ContainsKey(ModelsOption))
        {
            return "Name a run: --check, --draft or --models.";
        }

        var edge = new RecipeImportOptions().MaxImageEdge;
        if (values.TryGetValue(EdgeOption, out var edgeText)
            && !(int.TryParse(edgeText, NumberStyles.None, CultureInfo.InvariantCulture, out edge) && edge > 0))
        {
            return $"--edge takes a whole number of pixels, not {edgeText}.";
        }

        IReadOnlyList<string> models = values.TryGetValue(ModelsOption, out var list)
            ? list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : draft ? [DraftModel] : ComparedModels;
        if (models.Count == 0)
        {
            return "--models names no model.";
        }

        if (models.FirstOrDefault(model => ModelProfiles.Find(model) is null) is { } unknown)
        {
            return $"{unknown} has no price profile in ModelProfiles, so the site could not read with it.";
        }

        if (models.Distinct(StringComparer.Ordinal).Count() != models.Count)
        {
            return "--models names a model twice.";
        }

        if (draft && models.Count != 1)
        {
            return "--draft reads with one model.";
        }

        options = new EvalOptions(check ? EvalMode.Check : draft ? EvalMode.Draft : EvalMode.Compare, models, edge);
        return null;
    }
}
