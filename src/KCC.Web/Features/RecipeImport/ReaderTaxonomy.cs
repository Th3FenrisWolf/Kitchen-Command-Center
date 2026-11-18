using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.RecipeImport;

public sealed class ReaderTaxonomy
{
    private const string CategoriesFollow =
        "The site's categories follow, one a line, each with its id and name. Choose at most one:";

    private const string TagsFollow = "The site's tags follow, one a line, each with its id, name and group:";

    // The lists go into a prompt, never into HTML, so names keep their apostrophes and accents as typed.
    private static readonly JsonSerializerOptions LineJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly Dictionary<string, CategoryChoice> categoriesById;
    private readonly Dictionary<string, TagChoice> tagsById;

    private ReaderTaxonomy(IReadOnlyList<CategoryChoice> categories, IReadOnlyList<TagChoice> tags)
    {
        var categoryEntries = WithIds("C", categories);
        var tagEntries = WithIds("T", tags);
        categoriesById = categoryEntries.ToDictionary(entry => entry.Id, entry => entry.Choice, StringComparer.Ordinal);
        tagsById = tagEntries.ToDictionary(entry => entry.Id, entry => entry.Choice, StringComparer.Ordinal);
        Text = string.Join(
            '\n',
            [.. Section(CategoriesFollow, categoryEntries, CategoryLine), .. Section(TagsFollow, tagEntries, TagLine)]);
    }

    public string Text { get; }

    public int Characters => Text.Length;

    public static ReaderTaxonomy From(DetailsChoices choices, string flow) =>
        new(flow == ImportFlows.Variant ? [] : choices.Categories, choices.Tags);

    public CategoryChoice Category(string id) =>
        id is not null && categoriesById.TryGetValue(id, out var category) ? category : null;

    public TagChoice Tag(string id) => id is not null && tagsById.TryGetValue(id, out var tag) ? tag : null;

    public string CategoryId(string name) => IdOf(categoriesById, category => category.Name, name);

    public string TagId(string name) => IdOf(tagsById, tag => tag.Name, name);

    private static string IdOf<T>(Dictionary<string, T> choicesById, Func<T, string> nameOf, string name) =>
        choicesById.FirstOrDefault(entry => string.Equals(nameOf(entry.Value), name, StringComparison.OrdinalIgnoreCase)).Key;

    private static List<(string Id, T Choice)> WithIds<T>(string prefix, IReadOnlyList<T> choices) =>
        [.. choices.Select((choice, index) => (string.Create(CultureInfo.InvariantCulture, $"{prefix}{index + 1}"), choice))];

    private static IEnumerable<string> Section<T>(
        string heading,
        List<(string Id, T Choice)> entries,
        Func<string, T, string> line) =>
        entries.Count == 0 ? [] : [heading, .. entries.Select(entry => line(entry.Id, entry.Choice))];

    private static string CategoryLine(string id, CategoryChoice category) =>
        JsonSerializer.Serialize(new { id, name = category.Name }, LineJson);

    private static string TagLine(string id, TagChoice tag) =>
        JsonSerializer.Serialize(new { id, name = tag.Name, group = tag.Kind.ToLowerInvariant() }, LineJson);
}
