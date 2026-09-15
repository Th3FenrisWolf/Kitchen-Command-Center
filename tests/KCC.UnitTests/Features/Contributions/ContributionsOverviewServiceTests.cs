using KCC.Contributions.Admin;
using KCC.Contributions.Admin.Overview;
using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class ContributionsOverviewServiceTests
{
    [Test]
    public async Task NormalizeQuery_ClampsPageAndTrimsSearch()
    {
        var query = ContributionsOverviewService.NormalizeQuery(new RecipeOverviewQueryArgs
        {
            Page = -3,
            SearchText = "  mac  ",
            MaxAverageRating = 9,
        });

        _ = await Assert.That(query.Page).IsEqualTo(0);
        _ = await Assert.That(query.SearchText).IsEqualTo("mac");
        _ = await Assert.That(query.MaxAverageRating).IsEqualTo(5d);
        _ = await Assert.That(query.PageSize).IsEqualTo(ContributionsOverviewService.RecipePageSize);
    }

    [Test]
    public async Task NormalizeQuery_TreatsBlankSearchAsNull()
    {
        var query = ContributionsOverviewService.NormalizeQuery(new RecipeOverviewQueryArgs { SearchText = "   " });

        _ = await Assert.That(query.SearchText).IsNull();
        _ = await Assert.That(query.MaxAverageRating).IsNull();
    }

    [Test]
    public async Task CombineDistinct_DeduplicatesAcrossSourcesAndDropsEmpty()
    {
        var shared = Guid.NewGuid();
        var onlyReviews = Guid.NewGuid();
        var onlyCooked = Guid.NewGuid();

        var combined = ContributionsOverviewService.CombineDistinct(
            [shared, onlyReviews, Guid.Empty],
            [shared],
            [onlyCooked, shared]);

        _ = await Assert.That(combined.Count).IsEqualTo(3);
        _ = await Assert.That(combined).Contains(shared);
        _ = await Assert.That(combined).Contains(onlyReviews);
        _ = await Assert.That(combined).Contains(onlyCooked);
    }

    [Test]
    public async Task BuildVariantRows_ZeroFillsMissingAggregatesAndSortsByName()
    {
        var rated = Guid.NewGuid();
        var bare = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [rated] = "Zesty", [bare] = "Apple" };
        var ratings = new Dictionary<Guid, RatingAggregate> { [rated] = new(4.5, 2) };
        var noteCounts = new Dictionary<Guid, int> { [rated] = 3 };
        var cookedCounts = new Dictionary<Guid, int>();

        var rows = ContributionsOverviewService.BuildVariantRows([rated, bare], names, ratings, noteCounts, cookedCounts);

        _ = await Assert.That(rows[0].VariantName).IsEqualTo("Apple");
        _ = await Assert.That(rows[0].AverageRating).IsNull();
        _ = await Assert.That(rows[0].ReviewCount).IsEqualTo(0);
        _ = await Assert.That(rows[0].NoteCount).IsEqualTo(0);
        _ = await Assert.That(rows[1].VariantName).IsEqualTo("Zesty");
        _ = await Assert.That(rows[1].AverageRating).IsEqualTo(4.5);
        _ = await Assert.That(rows[1].NoteCount).IsEqualTo(3);
        _ = await Assert.That(rows[1].CookedCount).IsEqualTo(0);
    }

    [Test]
    public async Task BuildVariantRows_FallsBackToDeletedForUnknownNames()
    {
        var orphan = Guid.NewGuid();
        var rows = ContributionsOverviewService.BuildVariantRows(
            [orphan],
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, RatingAggregate>(),
            new Dictionary<Guid, int>(),
            new Dictionary<Guid, int>());

        _ = await Assert.That(rows[0].VariantName).IsEqualTo("(deleted)");
    }

    [Test]
    public async Task Snippet_CollapsesWhitespaceAndTruncates()
    {
        _ = await Assert.That(ContributionsOverviewService.Snippet(null)).IsEqualTo(string.Empty);
        _ = await Assert.That(ContributionsOverviewService.Snippet("  one\n two\tthree  ")).IsEqualTo("one two three");

        var longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var snippet = ContributionsOverviewService.Snippet(longText);
        _ = await Assert.That(snippet.Length).IsLessThanOrEqualTo(201);
        _ = await Assert.That(snippet.EndsWith('…')).IsTrue();
    }

    [Test]
    public async Task ToAdminUrl_PrefixesAdminPathWithoutDoublingSlashes()
    {
        _ = await Assert.That(AdminPath.ToAdminUrl("/community-contributions/reviews/12/edit"))
            .IsEqualTo("/admin/community-contributions/reviews/12/edit");
        _ = await Assert.That(AdminPath.ToAdminUrl("community-contributions/cook-notes/3/edit"))
            .IsEqualTo("/admin/community-contributions/cook-notes/3/edit");
    }

    /// <remarks>
    /// Link generation for a page under the Web pages application returns the prefix already applied,
    /// unlike one under a custom application. Prefixing it again produced /admin/admin/... links that
    /// 404 inside the administration.
    /// </remarks>
    [Test]
    public async Task ToAdminUrl_LeavesAnAlreadyPrefixedPathAlone()
    {
        _ = await Assert.That(AdminPath.ToAdminUrl("/admin/webpages-1/en_1181/variant-contributions"))
            .IsEqualTo("/admin/webpages-1/en_1181/variant-contributions");
        _ = await Assert.That(AdminPath.ToAdminUrl("admin/webpages-1/en_1181/variant-contributions"))
            .IsEqualTo("/admin/webpages-1/en_1181/variant-contributions");
    }

    /// <remarks>
    /// A slug that merely starts with the prefix's letters is a different application, not a prefixed
    /// path.
    /// </remarks>
    [Test]
    public async Task ToAdminUrl_PrefixesASlugThatOnlyLooksLikeThePrefix()
    {
        _ = await Assert.That(AdminPath.ToAdminUrl("administration/overview"))
            .IsEqualTo("/admin/administration/overview");
    }

    [Test]
    public async Task EscapeLikePattern_EscapesWildcards()
    {
        _ = await Assert.That(SqlRecipeRollupSource.EscapeLikePattern(@"100% real_deal [x] \o/"))
            .IsEqualTo(@"100\% real\_deal \[x] \\o/");
    }
}
