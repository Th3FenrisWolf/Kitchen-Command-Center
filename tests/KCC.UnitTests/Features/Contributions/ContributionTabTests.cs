using KCC.Contributions.Admin.WebPageTabs;
using Kentico.Xperience.Admin.Websites;

namespace KCC.UnitTests.Features.Contributions;

/// <remarks>
/// The section resolves and labels its object before the form beneath it validates anything, so the
/// label is the one place a contribution on another variant could still be read from — by author name,
/// on the breadcrumb, behind any id typed into the URL.
/// </remarks>
public class ContributionEntrySectionLabelTests
{
    private static readonly Guid ThisVariant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherVariant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Test]
    public async Task IsOnThisVariant_AcceptsAnEntryLeftAgainstThePageInTheUrl()
    {
        var lookup = new StubPageLookup(new ContributionPageIdentity(ContributionContentTypes.RecipeVariant, ThisVariant, 7));

        _ = await Assert.That(VariantReviewEditSection.IsOnThisVariant(lookup, Identifier(), ThisVariant)).IsTrue();
    }

    [Test]
    public async Task IsOnThisVariant_RefusesAnEntryLeftAgainstAnother()
    {
        var lookup = new StubPageLookup(new ContributionPageIdentity(ContributionContentTypes.RecipeVariant, ThisVariant, 7));

        _ = await Assert.That(VariantReviewEditSection.IsOnThisVariant(lookup, Identifier(), OtherVariant)).IsFalse();
    }

    [Test]
    public async Task IsOnThisVariant_RefusesAPageThatIsNotAVariant()
    {
        var lookup = new StubPageLookup(new ContributionPageIdentity(ContributionContentTypes.Recipe, ThisVariant, 0));

        _ = await Assert.That(VariantReviewEditSection.IsOnThisVariant(lookup, Identifier(), ThisVariant)).IsFalse();
    }

    private static WebPageUrlIdentifier Identifier() => new("en", 42);

    private sealed class StubPageLookup(ContributionPageIdentity identity) : IContributionPageLookup
    {
        public ContributionPageIdentity Identify(int webPageItemId) => identity;

        public IReadOnlyList<ContributionVariantPage> GetVariantPages(int recipeWebPageItemId) => [];
    }
}

public class ContributionTabAggregationTests
{
    private static readonly Guid VariantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid VariantB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Early = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Late = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Aggregate_ReportsNoRatingWhenOnlyNotesAndCookedMarksExist()
    {
        var rollup = ContributionTabService.Aggregate(
            [],
            [(VariantA, Early)],
            [(VariantA, Late)]);

        _ = await Assert.That(rollup.Totals.AverageRating).IsNull();
        _ = await Assert.That(rollup.Totals.NoteCount).IsEqualTo(1);
        _ = await Assert.That(rollup.Totals.CookedCount).IsEqualTo(1);
        _ = await Assert.That(rollup.Totals.LastActivity).IsEqualTo(Late);
    }

    [Test]
    public async Task Aggregate_SplitsTotalsPerVariant()
    {
        var rollup = ContributionTabService.Aggregate(
            [(VariantA, 5m, Early), (VariantA, 3m, Late), (VariantB, 1m, Early)],
            [(VariantB, Late)],
            []);

        _ = await Assert.That(rollup.Totals.ReviewCount).IsEqualTo(3);
        _ = await Assert.That(rollup.Totals.AverageRating).IsEqualTo(3d);
        _ = await Assert.That(rollup.ByVariant[VariantA].AverageRating).IsEqualTo(4d);
        _ = await Assert.That(rollup.ByVariant[VariantA].NoteCount).IsEqualTo(0);
        _ = await Assert.That(rollup.ByVariant[VariantB].AverageRating).IsEqualTo(1d);
        _ = await Assert.That(rollup.ByVariant[VariantB].NoteCount).IsEqualTo(1);
    }

    [Test]
    public async Task Aggregate_TakesLastActivityFromWhicheverTableIsNewest()
    {
        var rollup = ContributionTabService.Aggregate(
            [(VariantA, 5m, Early)],
            [],
            [(VariantA, Late)]);

        _ = await Assert.That(rollup.ByVariant[VariantA].LastActivity).IsEqualTo(Late);
    }

    /// <remarks>
    /// The recipe totals count everything filed under the recipe, so a contribution whose variant
    /// page has since been deleted has to be reported somewhere or the rows will not add up to the
    /// header.
    /// </remarks>
    [Test]
    public async Task OrphanedTotals_CoverWhatTheLiveVariantsDoNot()
    {
        var rollup = ContributionTabService.Aggregate(
            [(VariantA, 4m, Early), (VariantB, 2m, Late)],
            [(VariantB, Late)],
            []);

        var orphaned = ContributionTabService.OrphanedTotals(rollup, [VariantA]);

        _ = await Assert.That(orphaned.ReviewCount).IsEqualTo(1);
        _ = await Assert.That(orphaned.NoteCount).IsEqualTo(1);
        _ = await Assert.That(orphaned.AverageRating).IsEqualTo(2d);
        _ = await Assert.That(orphaned.LastActivity).IsEqualTo(Late);
    }

    [Test]
    public async Task OrphanedTotals_AreEmptyWhenEveryVariantIsStillLive()
    {
        var rollup = ContributionTabService.Aggregate([(VariantA, 4m, Early)], [], []);

        _ = await Assert.That(ContributionTabService.OrphanedTotals(rollup, [VariantA]).IsEmpty).IsTrue();
    }

    /// <remarks>
    /// The orphan average is weighted by review count: averaging the per-variant averages would let a
    /// deleted variant with one 1-star review outweigh one with twenty 5-star reviews.
    /// </remarks>
    [Test]
    public async Task OrphanedTotals_WeightTheAverageByReviewCount()
    {
        var rollup = ContributionTabService.Aggregate(
            [(VariantA, 5m, Early), (VariantA, 5m, Early), (VariantA, 5m, Early), (VariantB, 1m, Early)],
            [],
            []);

        var orphaned = ContributionTabService.OrphanedTotals(rollup, []);

        _ = await Assert.That(orphaned.AverageRating).IsEqualTo(4d);
    }
}
