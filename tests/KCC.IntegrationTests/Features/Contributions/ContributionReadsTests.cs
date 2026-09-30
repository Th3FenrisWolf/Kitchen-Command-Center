using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionReadsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContributionWrites Writes => Site.Services.GetRequiredService<IContributionWrites>();

    private IContributionReads Reads => Site.Services.GetRequiredService<IContributionReads>();

    private IContributionStats Stats => Site.Services.GetRequiredService<IContributionStats>();

    [Test]
    public async Task UpsertReview_ShowsInTheNextStatsRead()
    {
        var variantKey = Guid.NewGuid();
        _ = (await Stats.GetAsync()).For(variantKey);

        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 4.5m, "Crispy edges");

        _ = await Assert.That((await Stats.GetAsync()).For(variantKey).Rating).IsEqualTo(new RatingAggregate(4.5d, 1));
    }

    [Test]
    public async Task UpsertReview_ByTheSameMember_ReplacesTheirReview()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        await Writes.UpsertReviewAsync(variantKey, memberKey, 2m, "Too dry");
        await Writes.UpsertReviewAsync(variantKey, memberKey, 4m, "Better with butter");
        var reviews = await Reads.ReviewsAsync(variantKey, 0, 10);

        _ = await Assert.That(reviews.Total).IsEqualTo(1);
        _ = await Assert.That(reviews.Items.Single().Rating).IsEqualTo(4m);
        _ = await Assert.That(reviews.Items.Single().Text).IsEqualTo("Better with butter");
    }

    [Test]
    public async Task UpsertReview_WithAnOffStepRating_IsRefused()
    {
        _ = await Assert.That(async () => await Writes.UpsertReviewAsync(Guid.NewGuid(), Guid.NewGuid(), 3.7m, null))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Reviews_ReadBackAsUtc()
    {
        var variantKey = Guid.NewGuid();
        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 5m, "Crisp");

        var review = (await Reads.ReviewsAsync(variantKey, 0, 1)).Items.Single();

        _ = await Assert.That(review.Created.Kind).IsEqualTo(DateTimeKind.Utc);
        _ = await Assert.That(review.Modified.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    [Test]
    public async Task Reviews_ArePagedNewestFirst()
    {
        var variantKey = Guid.NewGuid();
        foreach (var text in new[] { "first", "second", "third" })
        {
            await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 5m, text);
        }

        var page = await Reads.ReviewsAsync(variantKey, 0, 2);

        _ = await Assert.That(page.Total).IsEqualTo(3);
        _ = await Assert.That(string.Join(",", page.Items.Select(review => review.Text))).IsEqualTo("third,second");
    }

    [Test]
    public async Task MemberReview_FindsOnlyThatMembersReview()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 3m, "Someone else");
        await Writes.UpsertReviewAsync(variantKey, memberKey, 5m, "Mine");

        var mine = await Reads.MemberReviewAsync(variantKey, memberKey);

        _ = await Assert.That(mine!.Text).IsEqualTo("Mine");
        _ = await Assert.That(await Reads.MemberReviewAsync(variantKey, Guid.NewGuid())).IsNull();
    }

    [Test]
    public async Task Notes_ArePagedNewestFirst()
    {
        var variantKey = Guid.NewGuid();
        var start = DateTime.UtcNow;
        await WriteAsync(db => db.CookNotes.AddRange(
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "first", Created = start, Modified = start },
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "second", Created = start.AddSeconds(1), Modified = start },
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "third", Created = start.AddSeconds(2), Modified = start }));

        var page = await Reads.NotesAsync(variantKey, 0, 2);

        _ = await Assert.That(page.Total).IsEqualTo(3);
        _ = await Assert.That(string.Join(",", page.Items.Select(note => note.Text))).IsEqualTo("third,second");
    }

    [Test]
    public async Task HasCooked_IsTrueOnlyForTheMemberWhoMarkedIt()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.CookedMarks.Add(new CookedMark { VariantKey = variantKey, MemberKey = memberKey, Created = DateTime.UtcNow }));

        _ = await Assert.That(await Reads.HasCookedAsync(variantKey, memberKey)).IsTrue();
        _ = await Assert.That(await Reads.HasCookedAsync(variantKey, Guid.NewGuid())).IsFalse();
    }

    private async Task WriteAsync(Action<ContributionsDbContext> change)
    {
        using var scope = Site.Services.GetRequiredService<IEFCoreScopeProvider<ContributionsDbContext>>().CreateScope();
        scope.WriteLock(ContributionLocks.Contributions);
        await scope.ExecuteWithContextAsync<Task>(async db =>
        {
            change(db);
            await db.SaveChangesAsync();
        });
        scope.Complete();
    }
}
