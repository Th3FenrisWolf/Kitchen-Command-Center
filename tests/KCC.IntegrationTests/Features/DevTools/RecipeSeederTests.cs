using System.Net;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.DevTools;

public class RecipeSeederTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Seeder_PublishesEveryRecipeAndVariantUnderTheListing()
    {
        var navigation = Site.Services.GetRequiredService<IDocumentNavigationQueryService>();
        var content = Site.Services.GetRequiredService<IContentService>();

        var recipeKeys = RecipeSeedData.Recipes.Select(recipe => SeedKeys.Recipe(recipe.Name)).ToList();
        var variantKeys = RecipeSeedData.Recipes
            .SelectMany(recipe => recipe.Variants.Select(variant => SeedKeys.Variant(recipe.Name, variant.Name)))
            .ToList();
        _ = navigation.TryGetChildrenKeysOfType(TestContent.RecipeListing(Site.Services), "recipe", out var listed);

        _ = await Assert.That(recipeKeys.All(listed.Contains)).IsTrue();
        _ = await Assert.That(content.GetByIds(recipeKeys).Count(recipe => recipe.Published)).IsEqualTo(36);
        _ = await Assert.That(content.GetByIds(variantKeys).Count(variant => variant.Published)).IsEqualTo(43);
    }

    [Test]
    public async Task Seeder_GivesEveryRecipeItsOwnGeneratedPhoto()
    {
        var content = Site.Services.GetRequiredService<IContentService>();
        var media = Site.Services.GetRequiredService<IMediaService>();

        foreach (var recipe in RecipeSeedData.Recipes)
        {
            var imageKey = SeedKeys.Image(recipe.Name);
            var photo = media.GetById(imageKey);

            _ = await Assert.That(content.GetById(SeedKeys.Recipe(recipe.Name))!.GetValue<string>("image")).Contains(imageKey.ToString());
            _ = await Assert.That(photo!.ContentType.Alias).IsEqualTo(Constants.Conventions.MediaTypes.Image);
            _ = await Assert.That(photo.GetValue<int>(Constants.Conventions.Media.Width)).IsEqualTo(RecipeSeedImages.Width);
            _ = await Assert.That(photo.GetValue<int>(Constants.Conventions.Media.Height)).IsEqualTo(RecipeSeedImages.Height);
            _ = await Assert.That(photo.GetValue<string>(Constants.Conventions.Media.Extension)).IsEqualTo("jpg");
        }
    }

    [Test]
    public async Task SeedPhoto_IsTheSameJpegEveryRun_AndDiffersPerRecipe()
    {
        var first = RecipeSeedImages.Jpeg("Shakshuka");
        var info = Image.Identify(first);

        _ = await Assert.That(RecipeSeedImages.Jpeg("Shakshuka").SequenceEqual(first)).IsTrue();
        _ = await Assert.That(RecipeSeedImages.Jpeg("Mango Lassi").SequenceEqual(first)).IsFalse();
        _ = await Assert.That(info.Metadata.DecodedImageFormat).IsEqualTo(JpegFormat.Instance);
        _ = await Assert.That((info.Width, info.Height)).IsEqualTo((RecipeSeedImages.Width, RecipeSeedImages.Height));
    }

    [Test]
    public async Task Seeder_SpreadsTheCreateDatesForTheNewestSorts()
    {
        var content = Site.Services.GetRequiredService<IContentService>();
        var pancakes = content.GetById(SeedKeys.Recipe("Fluffy Buttermilk Pancakes"))!;
        var toast = content.GetById(SeedKeys.Recipe("Avocado Toast Supreme"))!;
        var stack = content.GetById(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack"))!;

        _ = await Assert.That(pancakes.CreateDate - toast.CreateDate).IsEqualTo(TimeSpan.FromDays(2));
        _ = await Assert.That(stack.CreateDate - pancakes.CreateDate).IsEqualTo(TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task Seeder_CreatesApprovedAuthorsWithTheirNames()
    {
        var priya = Site.Services.GetRequiredService<IMemberService>().GetByUsername("priya.balan")!;

        _ = await Assert.That(priya.Key).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(priya.IsApproved).IsTrue();
        _ = await Assert.That(priya.GetValue<string>("firstName")).IsEqualTo("Priya");
        _ = await Assert.That(priya.GetValue<string>("lastName")).IsEqualTo("Balan");
    }

    [Test]
    public async Task Seeder_CreatesTheApprovedE2EMember_WhoCanSignIn()
    {
        using var scope = Site.Services.CreateScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync("e2e-member");

        var signIn = await scope.ServiceProvider.GetRequiredService<SignInManager<MemberIdentityUser>>()
            .CheckPasswordSignInAsync(member!, "E2E-Member-Passw0rd", lockoutOnFailure: false);

        _ = await Assert.That(member!.IsApproved).IsTrue();
        _ = await Assert.That(signIn.Succeeded).IsTrue();
    }

    [Test]
    public async Task Seeder_ReviewsEachRecipesFirstVariant()
    {
        var stats = await Site.Services.GetRequiredService<IContributionStats>().GetAsync();

        _ = await Assert.That(stats.For(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack")).Rating)
            .IsEqualTo(new RatingAggregate(4.25d, 2));
        _ = await Assert.That(stats.For(SeedKeys.Variant("Spicy Ramen Flight", "Miso Vegan")).Rating.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Seeder_SecondRun_SkipsEveryRecipe()
    {
        using var client = Site.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var response = await client.PostAsync("/api/dev/seed-recipes", null);
        var body = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(body.Contains("recipes +0 (skipped 36)", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Seeder_Rerun_RestoresReviewsMissingFromExistingRecipes()
    {
        var recipe = RecipeSeedData.Recipes.First(candidate => candidate.Name != "Fluffy Buttermilk Pancakes" && candidate.Reviews.Length > 0 && candidate.Variants.Length > 0);
        var variantKey = SeedKeys.Variant(recipe.Name, recipe.Variants[0].Name);
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        var stats = Site.Services.GetRequiredService<IContributionStats>();
        for (var index = 0; index < recipe.Reviews.Length; index++)
        {
            _ = await writes.DeleteReviewAsync(variantKey, SeedKeys.Reviewer(recipe.Name, index));
        }

        _ = await Assert.That((await stats.GetAsync()).For(variantKey).Rating.Count).IsEqualTo(0);

        using var client = Site.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        using var response = await client.PostAsync("/api/dev/seed-recipes", null);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That((await stats.GetAsync()).For(variantKey).Rating.Count).IsEqualTo(recipe.Reviews.Length);
    }

    [Test]
    public async Task Seeder_DatesItsReviewsTheSameEveryRun()
    {
        using var client = Site.CreateClient();
        var variantKey = SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack");

        using var json = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync($"/api/variant/{variantKey}/reviews?page=0&pageSize=10"));
        var created = json.RootElement.GetProperty("reviews").EnumerateArray()
            .Select(review => DateTime.Parse(review.GetProperty("created").GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal))
            .ToList();

        _ = await Assert.That(created).IsEquivalentTo([SeedKeys.ReviewDate(1), SeedKeys.ReviewDate(0)]);
    }
}
