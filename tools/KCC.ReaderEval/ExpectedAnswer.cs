using System.Text.Json;
using System.Text.Json.Serialization;
using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public sealed record ExpectedAnswer(
    bool Handwritten,
    ExpectedRecipe Recipe,
    ExpectedVariant Variant,
    IReadOnlyList<ExpectedIngredient> Ingredients,
    IReadOnlyList<string> Steps,
    string Match = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<int, IReadOnlyList<string>> AcceptedSteps = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
    };

    public static ExpectedAnswer FromRead(ReadResponse read, bool handwritten) => new(
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
        read.Match?.RecipeName);

    public IEnumerable<string> StepTexts(int step) =>
        [Steps[step - 1], .. AcceptedSteps?.GetValueOrDefault(step) ?? []];

    public static ExpectedAnswer Load(string path) =>
        JsonSerializer.Deserialize<ExpectedAnswer>(File.ReadAllText(path), Json)
        ?? throw new JsonException($"{path} holds no expected answer.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json) + Environment.NewLine);

    private static ExpectedValue<string> Text(MarkedValue<string> value) => new(value.Value, IsStated(value.Mark));

    private static ExpectedValue<int?> Count(MarkedValue<int?> value) => new(value.Value, IsStated(value.Mark));

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
