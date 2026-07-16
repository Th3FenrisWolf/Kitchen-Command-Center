namespace KCC.Web.Features.Search;

public record RecipeSearchCriteria
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 48;
    public const int MaxTime = 60;

    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> Diets { get; init; } = [];
    public int TimeMin { get; init; }
    public int TimeMax { get; init; } = MaxTime;
    public string Sort { get; init; } = "relevant";
    public int Page { get; init; }
    public int PageSize { get; init; } = DefaultPageSize;

    public bool TimeActive => TimeMin > 0 || TimeMax < MaxTime;

    public RecipeSearchCriteria Normalized()
    {
        var (min, max) = TimeMin <= TimeMax ? (TimeMin, TimeMax) : (TimeMax, TimeMin);
        return this with
        {
            Query = (Query ?? string.Empty).Trim(),
            Page = Math.Max(0, Page),
            PageSize = Math.Clamp(PageSize <= 0 ? DefaultPageSize : PageSize, 1, MaxPageSize),
            TimeMin = Math.Clamp(min, 0, MaxTime),
            TimeMax = Math.Clamp(max, 0, MaxTime),
        };
    }

    /// <summary>Maps a sort key to a Lucene sort spec.</summary>
    /// <param name="sort">The sort key (relevant|rated|variants|recent).</param>
    /// <returns>The field name, whether descending, and whether to sort by relevance score.</returns>
    public static (string Field, bool Descending, bool ByScore) SortSpec(string sort) => sort switch
    {
        "rated" => (RecipeSearchConstants.FieldAverageRating, true, false),
        "variants" => (RecipeSearchConstants.FieldVariantCount, true, false),
        "recent" => (RecipeSearchConstants.FieldPublished, true, false),
        _ => (RecipeSearchConstants.FieldName, false, true),
    };
}
