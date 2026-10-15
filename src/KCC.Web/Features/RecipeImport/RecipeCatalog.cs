using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.RecipeImport;

public sealed class RecipeCatalog
{
    public const int MostEntries = 500;

    private const string ShortIdPrefix = "R";

    // The catalog goes into a prompt, never into HTML, so names keep their apostrophes and accents as typed.
    private static readonly JsonSerializerOptions LineJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly Dictionary<string, CatalogEntry> entriesByShortId;

    private RecipeCatalog(IReadOnlyList<RecipePageData> recipesInNameOrder)
    {
        Entries =
        [
            .. recipesInNameOrder
                .Take(MostEntries)
                .Select((page, index) => new CatalogEntry(
                    string.Create(CultureInfo.InvariantCulture, $"{ShortIdPrefix}{index + 1}"),
                    page)),
        ];
        Choices = [.. recipesInNameOrder.Select(page => new RecipeChoice(page.Recipe.Key, page.Recipe.Name))];
        Text = string.Join('\n', Entries.Select(Line));
        entriesByShortId = Entries.ToDictionary(entry => entry.ShortId, StringComparer.Ordinal);
    }

    public IReadOnlyList<CatalogEntry> Entries { get; }

    public IReadOnlyList<RecipeChoice> Choices { get; }

    public string Text { get; }

    public int Characters => Text.Length;

    public static RecipeCatalog From(IEnumerable<RecipePageData> recipes) => new(
    [
        .. recipes
            .OrderBy(page => page.Recipe.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(page => page.Recipe.Name ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(page => page.Recipe.Key),
    ]);

    public CatalogEntry Find(string shortId) =>
        shortId is not null && entriesByShortId.TryGetValue(shortId, out var entry) ? entry : null;

    public RecipeMatch Match(string shortId, string reason) =>
        Find(shortId) is { } entry ? new RecipeMatch(entry.Key, entry.Name, reason?.Trim() ?? string.Empty) : null;

    private static string Line(CatalogEntry entry) => JsonSerializer.Serialize(
        new { id = entry.ShortId, name = entry.Name, description = entry.Description, variants = entry.VariantNames },
        LineJson);
}

public sealed record CatalogEntry(string ShortId, RecipePageData Page)
{
    public const int DescriptionLimit = 160;

    public Guid Key => Page.Recipe.Key;

    public string Name => Page.Recipe.Name;

    public string Description => Cut(Page.Recipe.Description?.Trim() ?? string.Empty);

    public IReadOnlyList<string> VariantNames => [.. Page.Variants.Select(variant => variant.Name)];

    private static string Cut(string text)
    {
        if (text.Length <= DescriptionLimit)
        {
            return text;
        }

        var length = char.IsHighSurrogate(text[DescriptionLimit - 1]) ? DescriptionLimit - 1 : DescriptionLimit;
        return text[..length];
    }
}
