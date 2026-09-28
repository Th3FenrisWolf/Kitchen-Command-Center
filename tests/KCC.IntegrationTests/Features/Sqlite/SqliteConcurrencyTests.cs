using System.Collections.Concurrent;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.IntegrationTests.Features.Sqlite;

// Spec §14: parallel review writes, a content save and an index rebuild, repeated, with no lock errors and consistent
// counts. Members signing up, signing in and being approved race the same writes, because a member save is followed
// by Umbraco's relations update, which is the write that stalled on SQLite.
public class SqliteConcurrencyTests
{
    private const int Rounds = 5;
    private const int ReviewWriters = 4;
    private const int ReviewsPerWriter = 50;
    private const int MemberRepeats = 5;

    // Every write here takes well under a second. A transaction stuck on a stale snapshot retries for minutes, so a
    // round that outlives this limit is a lock failure.
    private static readonly TimeSpan RoundLimit = TimeSpan.FromSeconds(60);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ParallelWrites_FinishAndLeaveConsistentCounts()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Axolotl");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Stress Test");
        var members = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var userName = TestMembers.UniqueUserName("race");
            await TestMembers.ApprovedAsync(Site.Services, userName);
            members.Add(userName);
        }

        var reviewers = new ConcurrentBag<Guid>();
        var rebuilder = Site.Services.GetRequiredService<IRecipeIndexRebuilder>();

        for (var round = 0; round < Rounds; round++)
        {
            var edit = round;
            var work = new List<Task>();
            work.AddRange(Enumerable.Range(0, ReviewWriters).Select(_ => Task.Run(() => WriteReviewsAsync(variant, reviewers))));
            work.AddRange(members.Select(userName => Task.Run(() => SignInRepeatedlyAsync(userName))));
            work.Add(Task.Run(() => SignUpAndApproveAsync()));
            work.Add(Task.Run(() => EditAndPublishAsync(recipe, $"Edited in round {edit}.")));
            work.Add(Task.Run(rebuilder.Signal));

            await Task.WhenAll(work).WaitAsync(RoundLimit);
            await rebuilder.WhenCurrentAsync(CancellationToken.None);

            var stored = (await Site.Services.GetRequiredService<IContributionReads>().ReviewsAsync(variant, 0, 1)).Total;
            var cached = (await Site.Services.GetRequiredService<IContributionStats>().GetAsync()).For(variant).ReviewCount;
            var indexed = IndexedReviewCount("axolotl");
            _ = await Assert.That(stored).IsEqualTo(reviewers.Count);
            _ = await Assert.That(cached).IsEqualTo(reviewers.Count);
            _ = await Assert.That(indexed).IsEqualTo(reviewers.Count);
        }
    }

    private async Task WriteReviewsAsync(Guid variant, ConcurrentBag<Guid> reviewers)
    {
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        for (var index = 0; index < ReviewsPerWriter; index++)
        {
            var reviewer = Guid.NewGuid();
            await writes.UpsertReviewAsync(variant, reviewer, 4.5m, "Held up under load.");
            reviewers.Add(reviewer);
        }
    }

    // The member saves a successful sign-in, a failed attempt and a sign-out make, in the lock the account
    // endpoints take.
    private async Task SignInRepeatedlyAsync(string userName)
    {
        for (var repeat = 0; repeat < MemberRepeats; repeat++)
        {
            using var scope = Site.Services.CreateScope();
            var members = scope.ServiceProvider.GetRequiredService<UserManager<MemberIdentityUser>>();
            await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(async () =>
            {
                var member = await members.FindByNameAsync(userName) ?? throw new InvalidOperationException($"No member {userName}.");
                member.LastLoginDate = DateTime.UtcNow;
                Succeeded(await members.UpdateAsync(member));
                Succeeded(await members.AccessFailedAsync(member));
                Succeeded(await members.ResetAccessFailedCountAsync(member));
                Succeeded(await members.UpdateSecurityStampAsync(member));
            });
        }
    }

    private async Task SignUpAndApproveAsync()
    {
        var userName = TestMembers.UniqueUserName("joiner");
        Guid key;
        using (var scope = Site.Services.CreateScope())
        {
            var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();
            var member = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
            Succeeded(await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(() => members.CreateAsync(member, TestMembers.Password)));
            key = member.Key;
        }

        await TestMembers.ApproveAsync(Site.Services, key);
    }

    // The owner's Save and Publish in the backoffice goes through these two services, outside any lock of ours.
    private async Task EditAndPublishAsync(Guid recipe, string description)
    {
        using var scope = Site.Services.CreateScope();
        var updated = await scope.ServiceProvider.GetRequiredService<IContentEditingService>().UpdateAsync(
            recipe,
            new ContentUpdateModel
            {
                Variants = [new VariantModel { Name = "IT Axolotl" }],
                Properties =
                [
                    new PropertyValueModel { Alias = "description", Value = description },
                    new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                ],
            },
            Constants.Security.SuperUserKey);
        if (updated.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Editing the recipe failed: {updated.Status}.");
        }

        var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .PublishAsync(recipe, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing the recipe failed: {published.Status}.");
        }
    }

    private int IndexedReviewCount(string word) =>
        Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single().ReviewCount;

    private static void Succeeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
