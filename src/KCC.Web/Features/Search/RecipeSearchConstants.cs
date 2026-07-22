namespace KCC.Web.Features.Search;

public static class RecipeSearchConstants
{
    public const string IndexName = "RecipeSearch";
    public const string StrategyName = "RecipeSearch";

    // Full-text
    public const string FieldName = "Name";
    public const string FieldContent = "Content"; // description + ingredients + author, combined
    // Sortable copy of the name (SortedDocValues) — the full-text FieldName is analyzed and can't be
    // sorted on; this gives the default view a stable alphabetical tie-break.
    public const string FieldNameSort = "NameSort";
    // Stored (for rendering a card without a second fetch)
    public const string FieldSlug = "Slug";
    public const string FieldIcon = "Icon";
    public const string FieldCategory = "Category";
    public const string FieldStartedBy = "StartedBy";
    public const string FieldTags = "Tags"; // ';'-joined for storage
    // Sortable / rangeable (DocValues)
    public const string FieldFastestTime = "FastestTime";
    public const string FieldVariantCount = "VariantCount";
    public const string FieldAverageRating = "AverageRating";
    public const string FieldReviewCount = "ReviewCount";
    public const string FieldPublished = "Published"; // unix seconds

    // Facet dimensions
    public const string FacetCategory = "CategoryFacet";
    public const string FacetDiet = "DietFacet";
}
