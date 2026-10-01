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

// Declared in the order the client reads, because the SSR prop and the JSON API both serialize it as it is.
public record RecipeSearchResults
{
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public IReadOnlyList<RecipeSearchHit> Results { get; init; } = [];
    public RecipeFacetCounts Facets { get; init; } = new();
    public RecipeSearchHit Spotlight { get; init; } // null when there is no spotlight
}

// Keyed by taxonomy title.
public record RecipeFacetCounts
{
    public IReadOnlyDictionary<string, int> Category { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Diet { get; init; } = new Dictionary<string, int>();
}
