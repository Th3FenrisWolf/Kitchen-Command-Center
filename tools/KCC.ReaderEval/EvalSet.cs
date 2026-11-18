using System.Globalization;
using System.Text.Json;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;

namespace KCC.ReaderEval;

public static class EvalSet
{
    private const int MinimumNutritionCases = 3;
    private const int MinimumCategoryCases = 10;
    private const string CatalogFile = "catalog.json";
    private const string CategoryAlias = "icon";
    private const string TagAlias = "kind";

    private const string CatalogGuide =
        "It holds a list of the site's recipes, each with a name, a description and the names of its variants "
        + "(specs/001-photo-recipe-import/quickstart.md, \"Reader evaluation\").";

    private const string Guide = "Put each case in a folder of its own, with its photos in photos/ "
        + "(specs/001-photo-recipe-import/quickstart.md, \"Reader evaluation\").";

    private static readonly JsonSerializerOptions CatalogJson = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
    };

    public static string DefaultDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KCC.ReaderEval.csproj")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? Path.Combine(Directory.GetCurrentDirectory(), "tools", "KCC.ReaderEval", "set")
            : Path.Combine(directory.FullName, "set");
    }

    public static IReadOnlyList<string> DetailsProblems(IReadOnlyDictionary<string, ExpectedAnswer> answers)
    {
        var problems = new List<string>();
        problems.AddRange(answers
            .Where(answer => answer.Value.Details is null)
            .Select(answer => $"  {answer.Key} has no details block in its expected.json."));
        problems.AddRange(answers
            .Where(answer => answer.Value.Details is { Draft: true })
            .Select(answer => $"  {answer.Key}: its details block is still a draft. Confirm it, then delete its \"draft\": true."));
        var nullNutrition = answers.Where(answer => answer.Value.Details is { Nutrition: null }).Select(answer => answer.Key).ToHashSet();
        problems.AddRange(nullNutrition.Select(name =>
            $"  {name}: its details block has \"nutrition\": null. Give every nutrient a number or null instead."));
        problems.AddRange(answers
            .Where(answer => answer.Value.Details is not null)
            .SelectMany(answer => UnknownNames(answer.Value.Details).Select(unknown => $"  {answer.Key}: {unknown}")));
        var details = answers
            .Where(answer => !nullNutrition.Contains(answer.Key))
            .Select(answer => answer.Value.Details)
            .Where(block => block is not null)
            .ToList();
        var withNutrition = details.Where(block => block.ExpectsNutrition).ToList();
        if (withNutrition.Count < MinimumNutritionCases)
        {
            problems.Add(
                $"  {withNutrition.Count} cases expect a nutrition value, and the comparison needs at least {MinimumNutritionCases}.");
        }

        if (!withNutrition.Any(block => block.NutritionPrinted == ExpectedDetails.Converted))
        {
            problems.Add("  none of the cases expecting nutrition prints it per whole recipe or in kilojoules (nutritionPrinted \"converted\"), and the comparison needs at least one.");
        }

        var withCategory = details.Count(block => block.ExpectsCategory);
        if (withCategory < MinimumCategoryCases)
        {
            problems.Add($"  {withCategory} cases expect a category, and the comparison needs at least {MinimumCategoryCases}.");
        }

        return problems;
    }

    public static RecipeCatalog LoadCatalog(string directory, out string problem)
    {
        problem = null;
        var path = Path.Combine(directory, CatalogFile);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var recipes = JsonSerializer.Deserialize<List<CatalogRecipe>>(File.ReadAllText(path), CatalogJson) ?? [];
            if (recipes.Any(recipe => string.IsNullOrWhiteSpace(recipe?.Name)))
            {
                problem = $"Every recipe in {path} needs a name. {CatalogGuide}";
                return null;
            }

            return RecipeCatalog.From(recipes.Select(recipe => recipe.Page()));
        }
        catch (JsonException exception)
        {
            problem = $"{path} cannot be read: {exception.Message} {CatalogGuide}";
            return null;
        }
    }

    public static IReadOnlyList<EvalCase> Load(string directory, out string problem)
    {
        if (!Directory.Exists(directory))
        {
            problem = $"The evaluation set is missing. Create {directory}. {Guide}";
            return [];
        }

        var cases = Directory.GetDirectories(directory)
            .Where(path => !IsHidden(path))
            .Order(StringComparer.Ordinal)
            .Select(path => new EvalCase(Path.GetFileName(path), path, Photos(Path.Combine(path, "photos"))))
            .ToList();
        if (cases.Count == 0)
        {
            problem = $"The evaluation set at {directory} is empty. {Guide}";
            return [];
        }

        var withoutPhotos = cases.Where(evalCase => evalCase.Photos.Count == 0).Select(evalCase => evalCase.Name).ToList();
        if (withoutPhotos.Count > 0)
        {
            problem = $"These cases in {directory} have no photos: {string.Join(", ", withoutPhotos)}. {Guide}";
            return [];
        }

        problem = null;
        return cases;
    }

    private static IEnumerable<string> UnknownNames(ExpectedDetails block)
    {
        if (block.ExpectsCategory && !Names(CategoryAlias).Contains(block.Category, StringComparer.OrdinalIgnoreCase))
        {
            yield return $"the category {block.Category} is not one of the site's categories.";
        }

        foreach (var tag in (block.Tags ?? []).Where(tag => !Names(TagAlias).Contains(tag.Name, StringComparer.OrdinalIgnoreCase)))
        {
            yield return $"the tag {tag.Name} is not one of the site's tags.";
        }
    }

    private static IEnumerable<string> Names(string alias) =>
        TaxonomyDefaults.Values.Where(value => value.Alias == alias).Select(value => value.Name);

    private static List<string> Photos(string directory) =>
        Directory.Exists(directory)
            ? [.. Directory.GetFiles(directory)
                .Where(path => !IsHidden(path))
                .OrderBy(PageNumber)
                .ThenBy(path => path, StringComparer.Ordinal)]
            : [];

    private static int PageNumber(string path) =>
        int.TryParse(Path.GetFileNameWithoutExtension(path), NumberStyles.None, CultureInfo.InvariantCulture, out var page)
            ? page
            : int.MaxValue;

    private static bool IsHidden(string path) => Path.GetFileName(path).StartsWith('.');
}

public sealed record EvalCase(string Name, string Directory, IReadOnlyList<string> Photos)
{
    public string ExpectedPath => Path.Combine(Directory, "expected.json");

    public string DraftPath => Path.Combine(Directory, "expected.draft.json");

    public bool IsHandwritten => Name.StartsWith("handwritten", StringComparison.OrdinalIgnoreCase);
}

public sealed record CatalogRecipe(string Name, string Description, IReadOnlyList<string> Variants)
{
    public RecipePageData Page() => new(
        new RecipeRecord(Guid.NewGuid(), Name.Trim(), null, Description, null, null, null, null, default),
        [.. (Variants ?? []).Select(Variant)],
        null);

    private static VariantRecord Variant(string name) => new(
        Guid.NewGuid(),
        name,
        null,
        null,
        null,
        null,
        0,
        0,
        0,
        null,
        null,
        [],
        null,
        null,
        null,
        default);
}
