namespace KCC.Web.Features.Search;

public record RecipeSearchDocument
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string StartedBy { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Diets { get; init; } = [];
    public IReadOnlyList<string> Styles { get; init; } = [];
    public IReadOnlyList<string> IngredientNames { get; init; } = [];
    public int FastestTime { get; init; }
    public int VariantCount { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public long PublishedUnixSeconds { get; init; }

    public static string JoinTags(IReadOnlyList<string> tags) => string.Join(';', tags);

    public static string BuildContent(RecipeSearchDocument d) => string.Join(
        ' ',
        new[] { d.Description, d.Category, d.StartedBy, string.Join(' ', d.Diets), string.Join(' ', d.IngredientNames) }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    public static int FastestOf(IReadOnlyList<(int Prep, int Cook)> variants) =>
        variants.Count == 0 ? 0 : variants.Min(v => v.Prep + v.Cook);
}
