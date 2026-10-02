using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Each test changes only content it made, named with a word no seeded recipe uses, so its searches cannot collide
// with the seed or with each other.
public class RecipeIndexTriggerTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task PublishingARecipe_MakesItSearchable()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Quokka");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Classic");

        var hit = (await SearchWhenCurrentAsync("quokka")).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("IT Quokka");
        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/it-quokka/");
        _ = await Assert.That(hit.VariantCount).IsEqualTo(1);
    }

    [Test]
    public async Task UnpublishingAVariant_KeepsItsRecipeAndDropsItsReviews()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Wombat");
        var kept = await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        await writes.UpsertReviewAsync(kept, Guid.NewGuid(), 4m, "Good");
        await writes.UpsertReviewAsync(withdrawn, Guid.NewGuid(), 2m, "Meh");
        _ = await Assert.That((await SearchWhenCurrentAsync("wombat")).Results.Single().AverageRating).IsEqualTo(3d);

        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var hit = (await SearchWhenCurrentAsync("wombat")).Results.Single();
        _ = await Assert.That(hit.VariantCount).IsEqualTo(1);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
    }

    [Test]
    public async Task TrashingARecipe_TakesItOut()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Numbat");
        _ = await Assert.That((await SearchWhenCurrentAsync("numbat")).Total).IsEqualTo(1);

        await TestContent.TrashAsync(Site.Services, recipeKey);

        _ = await Assert.That((await SearchWhenCurrentAsync("numbat")).Total).IsEqualTo(0);
    }

    [Test]
    public async Task RenamingACategory_RelabelsItsRecipes()
    {
        var categoryKey = await TestContent.CategoryAsync(Site.Services, "IT Pantry");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Dingo", TestContent.Pick("category", categoryKey));
        try
        {
            _ = await Assert.That((await SearchWhenCurrentAsync("dingo")).Results.Single().Category).IsEqualTo("IT Pantry");

            await TestContent.RenameAsync(Site.Services, categoryKey, "IT Larder");

            var results = await SearchWhenCurrentAsync("dingo");
            _ = await Assert.That(results.Results.Single().Category).IsEqualTo("IT Larder");
            _ = await Assert.That(results.Facets.Category.GetValueOrDefault("IT Larder")).IsEqualTo(1);
            _ = await Assert.That(results.Facets.Category.ContainsKey("IT Pantry")).IsFalse();
        }
        finally
        {
            await TestContent.TrashAsync(Site.Services, recipeKey);
            await TestContent.TrashAsync(Site.Services, categoryKey);
        }
    }

    [Test]
    public async Task RenamingATag_RelabelsItsDiet()
    {
        var tagKey = await TestContent.TagAsync(Site.Services, "IT Crunchy");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Kookaburra");
        try
        {
            await TestContent.VariantAsync(Site.Services, recipeKey, "Classic", TestContent.Pick("tags", tagKey));
            _ = await Assert.That(string.Join(",", (await SearchWhenCurrentAsync("kookaburra")).Results.Single().Tags)).IsEqualTo("IT Crunchy");

            await TestContent.RenameAsync(Site.Services, tagKey, "IT Crispy");

            var results = await SearchWhenCurrentAsync("kookaburra");
            _ = await Assert.That(string.Join(",", results.Results.Single().Tags)).IsEqualTo("IT Crispy");
            _ = await Assert.That(results.Facets.Diet.GetValueOrDefault("IT Crispy")).IsEqualTo(1);
        }
        finally
        {
            await TestContent.TrashAsync(Site.Services, recipeKey);
            await TestContent.TrashAsync(Site.Services, tagKey);
        }
    }

    [Test]
    public async Task RenamingAnAuthor_RenamesStartedBy()
    {
        var authorKey = await TestContent.AuthorAsync(Site.Services, "it.platypus", "Ada", "Platypus");
        await TestContent.RecipeAsync(Site.Services, "IT Echidna", TestContent.Author(authorKey));
        _ = await Assert.That((await SearchWhenCurrentAsync("echidna")).Results.Single().StartedBy).IsEqualTo("Ada Platypus");

        TestContent.RenameAuthor(Site.Services, authorKey, "Ada", "Lovelace");

        _ = await Assert.That((await SearchWhenCurrentAsync("echidna")).Results.Single().StartedBy).IsEqualTo("Ada Lovelace");
        _ = await Assert.That((await SearchWhenCurrentAsync("lovelace")).Total).IsEqualTo(1);
        _ = await Assert.That((await SearchWhenCurrentAsync("platypus")).Total).IsEqualTo(0);
    }

    [Test]
    public async Task WritingAReview_RatesItsRecipe()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Bilby");
        var variantKey = await TestContent.VariantAsync(Site.Services, recipeKey, "Classic");
        _ = await Assert.That((await SearchWhenCurrentAsync("bilby")).Results.Single().ReviewCount).IsEqualTo(0);

        await Site.Services.GetRequiredService<IContributionWrites>().UpsertReviewAsync(variantKey, Guid.NewGuid(), 2.5m, "Fine");

        var hit = (await SearchWhenCurrentAsync("bilby")).Results.Single();
        _ = await Assert.That(hit.AverageRating).IsEqualTo(2.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
    }

    private async Task<RecipeSearchResults> SearchWhenCurrentAsync(string query)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = query });
    }
}
