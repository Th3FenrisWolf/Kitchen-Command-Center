using KCC.Contributions;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Sqlite;

// Once a trash has committed, Umbraco relates the trashed item to its old parent, reading before it writes, so a
// review committing in between strands that write on a stale snapshot.
public class TrashRaceTests
{
    private const int Rounds = 3;

    // Creating and trashing a recipe takes about a second here. A write stranded on a stale snapshot retries for about
    // ten minutes.
    private static readonly TimeSpan RoundLimit = TimeSpan.FromSeconds(20);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task TrashingWhileAReviewCommits_FinishesAndRecordsTheOldParent()
    {
        var probe = Site.Services.GetRequiredService<TrashRelationProbe>();
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        var listing = IdOf(TestContent.RecipeListing(Site.Services));
        for (var round = 0; round < Rounds; round++)
        {
            var name = $"IT Ocelot {round}";
            probe.CommitDuringNextTrash(() => writes.UpsertReviewAsync(Guid.NewGuid(), Guid.NewGuid(), 4.5m, "Written while the owner tidies up."));
            var recipe = await Task.Run(async () =>
            {
                var key = await TestContent.RecipeAsync(Site.Services, name);
                await TestContent.TrashAsync(Site.Services, key);
                return key;
            }).WaitAsync(RoundLimit);

            _ = await Assert.That(probe.Committed is not null).IsTrue();
            await probe.Committed!.WaitAsync(RoundLimit);
            _ = await Assert.That(OldParentOf(recipe)).IsEqualTo(listing);
        }
    }

    private int IdOf(Guid key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IContentService>().GetById(key)!.Id;
    }

    private int? OldParentOf(Guid key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IRelationService>()
            .GetByChildId(IdOf(key), Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias)
            .SingleOrDefault()?.ParentId;
    }
}
