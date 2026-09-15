using Kentico.Xperience.Admin.Base;

namespace KCC.Contributions.Admin.Overview;

public sealed class ContributionsOverviewPageProperties : TemplateClientProperties
{
    public int RecipePageSize { get; set; }
    public int EntryPageSize { get; set; }
}

public sealed class RecipeOverviewQueryArgs
{
    public int Page { get; set; }
    public string SearchText { get; set; }
    public double? MaxAverageRating { get; set; }
}

public sealed record RecipeOverviewRow(
    Guid RecipeGuid,
    string RecipeName,
    double? AverageRating,
    int ReviewCount,
    int NoteCount,
    int CookedCount,
    DateTime? LastActivity);

public sealed record RecipeOverviewPageResult(
    IReadOnlyList<RecipeOverviewRow> Recipes,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class RecipeVariantsArgs
{
    public Guid RecipeGuid { get; set; }
}

public sealed record VariantOverviewRow(
    Guid VariantGuid,
    string VariantName,
    double? AverageRating,
    int ReviewCount,
    int NoteCount,
    int CookedCount);

public sealed record RecipeVariantsResult(IReadOnlyList<VariantOverviewRow> Variants);

public sealed class VariantEntriesArgs
{
    public Guid VariantGuid { get; set; }
    public int Page { get; set; }
}

public sealed record ReviewEntry(
    int Id,
    string MemberName,
    decimal Rating,
    string TextSnippet,
    DateTime Created,
    string EditUrl);

public sealed record ReviewEntriesResult(
    IReadOnlyList<ReviewEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CookNoteEntry(
    int Id,
    string MemberName,
    string TextSnippet,
    DateTime Created,
    string EditUrl);

public sealed record CookNoteEntriesResult(
    IReadOnlyList<CookNoteEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class DeleteEntryArgs
{
    public int Id { get; set; }
}

public sealed record DeleteEntryResult(bool Deleted);
