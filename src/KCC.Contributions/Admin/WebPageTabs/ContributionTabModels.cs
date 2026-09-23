using Kentico.Xperience.Admin.Websites.UIPages;

namespace KCC.Contributions.Admin.WebPageTabs;

public sealed record ContributionTotals(
    double? AverageRating,
    int ReviewCount,
    int NoteCount,
    int CookedCount,
    DateTime? LastActivity)
{
    public static readonly ContributionTotals Empty = new(null, 0, 0, 0, null);

    public bool IsEmpty => ReviewCount == 0 && NoteCount == 0 && CookedCount == 0;
}

public sealed record RecipeContributionRollup(
    ContributionTotals Totals,
    IReadOnlyDictionary<Guid, ContributionTotals> ByVariant);

public sealed record VariantContributionRow(
    string VariantName,
    double? AverageRating,
    int ReviewCount,
    int NoteCount,
    int CookedCount,
    DateTime? LastActivity,
    string ContributionsUrl);

public sealed record ContributionEntry(
    int Id,
    string MemberName,
    decimal? Rating,
    string Text,
    DateTime Created,
    string EditUrl);

public sealed record ContributionEntryPage(IReadOnlyList<ContributionEntry> Entries, int TotalCount, int Page)
{
    public static readonly ContributionEntryPage Empty = new([], 0, 0);
}

/// <remarks>
/// A delete changes the rating average and the distribution as well as the list, and the header is
/// server-rendered — returning only the entries would leave a variant reading "5.0 (3)" over two
/// reviews until the tab was opened again.
/// </remarks>
public sealed record ContributionMutationResult(
    ContributionTotals Totals,
    IReadOnlyList<int> RatingDistribution,
    ContributionEntryPage Entries);

public sealed class RecipeContributionsClientProperties : WebPageBaseClientProperties
{
    public ContributionTotals Totals { get; set; } = ContributionTotals.Empty;

    public IEnumerable<VariantContributionRow> Variants { get; set; } = [];

    /// <summary>
    /// Contributions filed under this recipe against a variant that is no longer one of its pages,
    /// so that the totals above agree with the rows.
    /// </summary>
    public ContributionTotals Orphaned { get; set; } = ContributionTotals.Empty;
}

public sealed class VariantContributionsClientProperties : WebPageBaseClientProperties
{
    public ContributionTotals Totals { get; set; } = ContributionTotals.Empty;

    public IEnumerable<int> RatingDistribution { get; set; } = [];

    public string RecipeContributionsUrl { get; set; } = string.Empty;

    public int EntryPageSize { get; set; }

    public bool CanDelete { get; set; }

    public ContributionEntryPage Reviews { get; set; } = ContributionEntryPage.Empty;

    public ContributionEntryPage Notes { get; set; } = ContributionEntryPage.Empty;
}

public sealed class ContributionEntriesArgs
{
    public int Page { get; set; }
}

public sealed class ContributionEntryArgs
{
    public int Id { get; set; }
}
