using System.Text.Json;
using System.Text.Json.Serialization;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;

namespace KCC.ReaderEval;

public sealed record ExpectedAnswer(
    bool Handwritten,
    ExpectedRecipe Recipe,
    ExpectedVariant Variant,
    IReadOnlyList<ExpectedIngredient> Ingredients,
    IReadOnlyList<string> Steps,
    string Match = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<int, IReadOnlyList<string>> AcceptedSteps = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ExpectedDetails Details = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
    };

    public static ExpectedAnswer FromRead(
        ReadResponse read,
        bool handwritten,
        ReaderDetails raw = null,
        ReaderTaxonomy taxonomy = null) => new(
        handwritten,
        new ExpectedRecipe(Text(read.Recipe.Name), Text(read.Recipe.Description)),
        new ExpectedVariant(
            Text(read.Variant.Name),
            Text(read.Variant.Description),
            Count(read.Variant.PrepTime),
            Count(read.Variant.CookTime),
            Count(read.Variant.Servings)),
        [.. read.Ingredients.Select(row => new ExpectedIngredient(row.Name, row.Quantity, row.Unit))],
        [.. read.Instructions.Select(step => step.Text)],
        read.Match?.RecipeName,
        Details: DraftedDetails(read.Details, raw, taxonomy ?? EvalTaxonomy.Create()));

    public IEnumerable<string> StepTexts(int step) =>
        [Steps[step - 1], .. AcceptedSteps?.GetValueOrDefault(step) ?? []];

    public static ExpectedAnswer Load(string path) =>
        JsonSerializer.Deserialize<ExpectedAnswer>(File.ReadAllText(path), Json)
        ?? throw new JsonException($"{path} holds no expected answer.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json) + Environment.NewLine);

    private static ExpectedValue<string> Text(MarkedValue<string> value) => new(value.Value, IsStated(value.Mark));

    private static ExpectedValue<int?> Count(MarkedValue<int?> value) => new(value.Value, IsStated(value.Mark));

    private static ExpectedDetails DraftedDetails(DetailsValues details, ReaderDetails raw, ReaderTaxonomy taxonomy)
    {
        var statedByTag = new Dictionary<Guid, bool>();
        foreach (var tag in raw?.Tags ?? [])
        {
            if (taxonomy.Tag(tag?.Id) is { } choice)
            {
                statedByTag.TryAdd(choice.Key, tag.Stated);
            }
        }

        var nutrition = details?.Nutrition;
        var values = new ExpectedNutrition(
            nutrition?.Calories.Value,
            nutrition?.ProteinG.Value,
            nutrition?.CarbsG.Value,
            nutrition?.FatG.Value,
            nutrition?.SaturatedFatG.Value,
            nutrition?.FiberG.Value,
            nutrition?.SugarG.Value,
            nutrition?.SodiumMg.Value);
        MarkedValue<int?>[] marked = nutrition is null
            ? []
            : [nutrition.Calories, nutrition.ProteinG, nutrition.CarbsG, nutrition.FatG, nutrition.SaturatedFatG, nutrition.FiberG, nutrition.SugarG, nutrition.SodiumMg];
        var filled = marked.Where(value => value.Value is not null).ToList();
        return new ExpectedDetails(
            details?.Category.Value is { } category ? NameOf(category) : null,
            [.. (details?.Tags ?? []).Select(tag => new ExpectedTag(NameOf(tag.Key), statedByTag.GetValueOrDefault(tag.Key)))],
            details?.Difficulty.Value is { } level && IsNamed(details.Difficulty.Mark) ? level : null,
            filled.Count == 0 ? null : PrintedPerRecipeOrInKilojoules(nutrition.Calories, raw?.Nutrition) ? ExpectedDetails.Converted : ExpectedDetails.PerServing,
            values,
            Draft: true);
    }

    private static bool PrintedPerRecipeOrInKilojoules(MarkedValue<int?> calories, ReaderNutrition raw) =>
        raw?.Basis == "recipe"
        || raw?.Values?.Any(value => value?.Nutrient == "calories" && value.Unit == "kJ") == true
        || calories.Mark?.Arg?.Contains("kJ", StringComparison.OrdinalIgnoreCase) == true;

    private static string NameOf(Guid key) => TaxonomyDefaults.Values.FirstOrDefault(value => value.Key == key)?.Name ?? key.ToString();

    private static bool IsNamed(Mark mark) => mark?.Kind != MarkKinds.Suggested || mark.Code == MarkCodes.PrintedAs;

    private static bool IsStated(Mark mark) => mark?.Kind != MarkKinds.Estimate && mark?.Kind != MarkKinds.Written;
}

public sealed record ExpectedRecipe(ExpectedValue<string> Name, ExpectedValue<string> Description);

public sealed record ExpectedVariant(
    ExpectedValue<string> Name,
    ExpectedValue<string> Description,
    ExpectedValue<int?> PrepMinutes,
    ExpectedValue<int?> CookMinutes,
    ExpectedValue<int?> Servings);

public sealed record ExpectedValue<T>(T Value, bool Stated);

public sealed record ExpectedIngredient(
    string Name,
    decimal? Quantity,
    string Unit,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string> Accepted = null)
{
    [JsonIgnore]
    public IEnumerable<string> Names => [Name, .. Accepted ?? []];
}

public sealed record ExpectedDetails(
    string Category,
    IReadOnlyList<ExpectedTag> Tags,
    string Difficulty,
    string NutritionPrinted,
    ExpectedNutrition Nutrition,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Draft = false)
{
    public const string PerServing = "perServing";
    public const string Converted = "converted";

    public bool ExpectsCategory => !string.IsNullOrWhiteSpace(Category);

    public bool ExpectsNutrition => Nutrition.Values.Any(value => value is not null);
}

public sealed record ExpectedTag(string Name, bool Stated);

public sealed record ExpectedNutrition(
    int? Calories,
    int? ProteinG,
    int? CarbsG,
    int? FatG,
    int? SaturatedFatG,
    int? FiberG,
    int? SugarG,
    int? SodiumMg)
{
    [JsonIgnore]
    public IReadOnlyList<int?> Values => [Calories, ProteinG, CarbsG, FatG, SaturatedFatG, FiberG, SugarG, SodiumMg];
}
