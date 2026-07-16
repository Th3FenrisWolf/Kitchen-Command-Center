namespace KCC.Web.Features.Search;

public record RecipeSearchHit
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string StartedBy { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public double? AverageRating { get; init; } // null when ReviewCount == 0
    public int ReviewCount { get; init; }
    public int VariantCount { get; init; }
    public int FastestTime { get; init; }
}

public record RecipeSearchResults
{
    public IReadOnlyList<RecipeSearchHit> Results { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public IReadOnlyDictionary<string, int> CategoryFacets { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> DietFacets { get; init; } = new Dictionary<string, int>();
    public RecipeSearchHit Spotlight { get; init; } // null when there is no spotlight
}

/// <summary>One envelope shape shared by the SSR page prop and the JSON API, so the client
/// consumes an identical structure either way. camelCase names match Vue.Prop + System.Text.Json;
/// facet-dictionary keys are taxonomy titles, preserved as-is.</summary>
public static class RecipeSearchResponseMapper
{
    /// <summary>Projects service results into the wire/prop envelope.</summary>
    /// <param name="r">The search results to project.</param>
    /// <returns>An anonymous object with camelCase members for JSON/Vue.Prop.</returns>
    public static object ToResponse(RecipeSearchResults r) => new
    {
        total = r.Total,
        page = r.Page,
        pageSize = r.PageSize,
        results = r.Results.Select(Hit).ToArray(),
        facets = new { category = r.CategoryFacets, diet = r.DietFacets },
        spotlight = r.Spotlight is null ? null : Hit(r.Spotlight),
    };

    private static object Hit(RecipeSearchHit h) => new
    {
        name = h.Name,
        slug = h.Slug,
        icon = h.Icon,
        category = h.Category,
        startedBy = h.StartedBy,
        tags = h.Tags,
        averageRating = h.AverageRating,
        reviewCount = h.ReviewCount,
        variantCount = h.VariantCount,
        fastestTime = h.FastestTime,
    };
}
