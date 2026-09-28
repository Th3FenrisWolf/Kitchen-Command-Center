using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionCascadeTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContributionWrites Writes => Site.Services.GetRequiredService<IContributionWrites>();

    [Test]
    public async Task DeletingAVariant_DeletesItsReviewsNotesAndCookedMarks()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Aardvark");
        var doomed = await TestContent.VariantAsync(Site.Services, recipe, "Doomed");
        var kept = await TestContent.VariantAsync(Site.Services, recipe, "Kept");
        await ContributeAsync(doomed, Guid.NewGuid());
        await ContributeAsync(kept, Guid.NewGuid());

        await DeleteAsync(doomed);

        _ = await Assert.That(await RowsAsync(doomed)).IsEqualTo((0, 0, 0));
        _ = await Assert.That(await RowsAsync(kept)).IsEqualTo((1, 1, 1));
        _ = await Assert.That((await SearchWhenCurrentAsync("aardvark")).ReviewCount).IsEqualTo(1);
    }

    [Test]
    public async Task DeletingARecipe_DeletesItsVariantsRows()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Jerboa");
        var first = await TestContent.VariantAsync(Site.Services, recipe, "First");
        var second = await TestContent.VariantAsync(Site.Services, recipe, "Second");
        await ContributeAsync(first, Guid.NewGuid());
        await ContributeAsync(second, Guid.NewGuid());

        await DeleteAsync(recipe);

        _ = await Assert.That(await RowsAsync(first)).IsEqualTo((0, 0, 0));
        _ = await Assert.That(await RowsAsync(second)).IsEqualTo((0, 0, 0));
    }

    [Test]
    public async Task TrashingAVariant_KeepsItsRows_UntilTheRecycleBinIsEmptied()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Chinchilla");
        var trashed = await TestContent.VariantAsync(Site.Services, recipe, "Trashed");
        await ContributeAsync(trashed, Guid.NewGuid());

        await TestContent.TrashAsync(Site.Services, trashed);
        var whileTrashed = await RowsAsync(trashed);
        using (var scope = Site.Services.CreateScope())
        {
            _ = await scope.ServiceProvider.GetRequiredService<IContentService>().EmptyRecycleBinAsync(Constants.Security.SuperUserKey);
        }

        _ = await Assert.That(whileTrashed).IsEqualTo((1, 1, 1));
        _ = await Assert.That(await RowsAsync(trashed)).IsEqualTo((0, 0, 0));
    }

    [Test]
    public async Task DeletingAMember_DeletesTheirRowsOnly()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Dormouse");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var leaving = await TestMembers.ApprovedAsync(Site.Services, TestMembers.UniqueUserName("leaving"));
        var staying = await TestMembers.ApprovedAsync(Site.Services, TestMembers.UniqueUserName("staying"));
        await ContributeAsync(variant, leaving);
        await ContributeAsync(variant, staying);

        using (var scope = Site.Services.CreateScope())
        {
            var deleted = await scope.ServiceProvider.GetRequiredService<IMemberEditingService>().DeleteAsync(leaving, Constants.Security.SuperUserKey);
            _ = await Assert.That(deleted.Success).IsTrue();
        }

        _ = await Assert.That(await RowsAsync(variant)).IsEqualTo((1, 1, 1));
        _ = await Assert.That(await Site.Services.GetRequiredService<IContributionReads>().MemberReviewAsync(variant, leaving)).IsNull();
        _ = await Assert.That(await Site.Services.GetRequiredService<IContributionReads>().HasCookedAsync(variant, staying)).IsTrue();
    }

    private async Task ContributeAsync(Guid variant, Guid member)
    {
        await Writes.UpsertReviewAsync(variant, member, 4m, "Good.");
        _ = await Writes.AddNoteAsync(variant, member, "Noted.");
        await Writes.MarkCookedAsync(variant, member);
    }

    private async Task DeleteAsync(Guid key)
    {
        using var scope = Site.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentEditingService>().DeleteAsync(key, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Deleting {key} failed: {result.Status}.");
        }
    }

    private async Task<(int Reviews, int Notes, int Cooked)> RowsAsync(Guid variant)
    {
        var reads = Site.Services.GetRequiredService<IContributionReads>();
        var stats = (await Site.Services.GetRequiredService<IContributionStats>().GetAsync()).For(variant);
        return ((await reads.ReviewsAsync(variant, 0, 1)).Total, (await reads.NotesAsync(variant, 0, 1)).Total, stats.CookedCount);
    }

    private async Task<RecipeSearchHit> SearchWhenCurrentAsync(string word)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single();
    }
}
