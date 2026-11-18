using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Recipes;

public class RecipeQueriesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task GetRecipePage_ReadsASeededRecipe()
    {
        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Recipe.Url).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(page.Recipe.Category).IsEqualTo("Lunch");
        _ = await Assert.That(page.Recipe.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(page.Recipe.ImageUrl).Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}");
        _ = await Assert.That(page.Variants.Count).IsEqualTo(4);
        _ = await Assert.That(page.AddVariantUrl).IsEqualTo("/recipes/add-variant/");
    }

    [Test]
    public async Task GetRecipePage_ReadsEachVariant()
    {
        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        var shoyu = page.Variants.Single(variant => variant.Name == "Chili Oil Shoyu");

        _ = await Assert.That(shoyu.Url).IsEqualTo("/recipes/spicy-ramen-flight/chili-oil-shoyu/");
        _ = await Assert.That(shoyu.TotalTime).IsEqualTo(20);
        _ = await Assert.That(shoyu.Servings).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", shoyu.Tags)).IsEqualTo("Spicy");
        _ = await Assert.That(shoyu.Nutrition.Calories).IsNull();
        _ = await Assert.That(shoyu.Difficulty).IsNull();
        _ = await Assert.That(shoyu.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(shoyu.IngredientsJson.Contains("\"Chili Oil\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GetVariantPage_ReadsTheRecipeAndTheOtherVariants()
    {
        var page = WithContent((content, queries) => queries.GetVariantPage(
            (RecipeVariant)content.GetById(SeedKeys.Variant("Spicy Ramen Flight", "Chili Oil Shoyu"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Siblings.Count).IsEqualTo(3);
        _ = await Assert.That(page.Siblings.Any(sibling => sibling.Name == "Chili Oil Shoyu")).IsFalse();
    }

    [Test]
    public async Task GetRecipePage_LeavesOutAnUnpublishedVariant()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Half Published");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(recipeKey)!));

        _ = await Assert.That(string.Join(",", page.Variants.Select(variant => variant.Name))).IsEqualTo("Kept");
    }

    [Test]
    public async Task RecipeImage_IsAWebpTileBrowsersKeepForAYear()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Stack photo");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Pictured", TestContent.Image("image", imageKey));

        var tile = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(recipeKey)!)).Recipe.ImageUrl;
        using var client = Site.CreateClient();
        using var image = await client.GetAsync(tile);

        _ = await Assert.That(tile.Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(tile.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(image.Content.Headers.ContentType!.MediaType).IsEqualTo("image/webp");
        _ = await Assert.That(image.Headers.CacheControl!.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
    }

    [Test]
    public async Task GetPublishedRecipes_ListsEveryPublishedRecipeWithItsVariants()
    {
        var withdrawn = await TestContent.RecipeAsync(Site.Services, "IT Never Listed");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var recipes = WithContent((_, queries) => queries.GetPublishedRecipes());

        _ = await Assert.That(recipes.Count(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight"))).IsEqualTo(1);
        _ = await Assert.That(recipes.Single(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight")).Variants.Count).IsEqualTo(4);
        _ = await Assert.That(recipes.Single(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight")).AddVariantUrl).IsEqualTo("/recipes/add-variant/");
        _ = await Assert.That(recipes.Count(page => SeedKeys.Recipe(page.Recipe.Name) == page.Recipe.Key)).IsEqualTo(36);
        _ = await Assert.That(recipes.Any(page => page.Recipe.Key == withdrawn)).IsFalse();
    }

    [Test]
    public async Task GetTaxonomy_ListsTheCategoriesAndTheTagsByKind_InTreeOrder()
    {
        var taxonomy = WithContent((_, queries) => queries.GetTaxonomy());

        _ = await Assert.That(string.Join(",", taxonomy.Categories)).IsEqualTo("Breakfast,Lunch,Dinner,Dessert,Snack,Beverage");
        _ = await Assert.That(string.Join(",", taxonomy.Diets)).IsEqualTo("Vegetarian,Vegan,Gluten-Free,Dairy-Free,Keto,High-Protein,Low-Carb");
        _ = await Assert.That(string.Join(",", taxonomy.Styles)).IsEqualTo("Spicy,Cheesy,Easy,Fast");
    }

    [Test]
    public async Task GetTaxonomyChoices_ListsTheCategoriesInTreeOrderWithTheirKeys()
    {
        var choices = WithContent((_, queries) => queries.GetTaxonomyChoices());

        _ = await Assert.That(string.Join(",", choices.Categories.Take(6).Select(category => category.Name))).IsEqualTo("Breakfast,Lunch,Dinner,Dessert,Snack,Beverage");
        _ = await Assert.That(choices.Categories.All(category => category.Key != Guid.Empty)).IsTrue();
    }

    [Test]
    public async Task GetTaxonomyChoices_ListsTheTagsWithTheirKind()
    {
        var spicy = WithContent((_, queries) => queries.GetTaxonomyChoices()).Tags.Single(tag => tag.Name == "Spicy");
        var vegan = WithContent((_, queries) => queries.GetTaxonomyChoices()).Tags.Single(tag => tag.Name == "Vegan");

        _ = await Assert.That(spicy.Kind).IsEqualTo(TagKinds.Style);
        _ = await Assert.That(vegan.Kind).IsEqualTo(TagKinds.Diet);
        _ = await Assert.That(spicy.Key).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task GetTaxonomyChoices_CountsATagWithNoStyleKindAsADiet()
    {
        var key = await TestContent.TagAsync(Site.Services, "IT Kindless Tag", kind: "");
        try
        {
            var tag = WithContent((_, queries) => queries.GetTaxonomyChoices()).Tags.Single(choice => choice.Key == key);

            _ = await Assert.That(tag.Kind).IsEqualTo(TagKinds.Diet);
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, key);
        }
    }

    [Test]
    public async Task GetTaxonomyChoices_LeavesOutAnUnpublishedCategoryAndTag()
    {
        var category = await TestContent.CategoryAsync(Site.Services, "IT Withdrawn Category");
        var tag = await TestContent.TagAsync(Site.Services, "IT Withdrawn Tag");
        try
        {
            await TestContent.UnpublishAsync(Site.Services, category);
            await TestContent.UnpublishAsync(Site.Services, tag);

            var choices = WithContent((_, queries) => queries.GetTaxonomyChoices());

            _ = await Assert.That(choices.Categories.Any(choice => choice.Key == category)).IsFalse();
            _ = await Assert.That(choices.Tags.Any(choice => choice.Key == tag)).IsFalse();
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, category, tag);
        }
    }

    [Test]
    public async Task GetTaxonomyChoices_ShowsACategoryAndATagAddedInTheBackoffice()
    {
        var category = await TestContent.CategoryAsync(Site.Services, "IT Fresh Category");
        var tag = await TestContent.TagAsync(Site.Services, "IT Fresh Tag", TagKinds.Style);
        try
        {
            var choices = WithContent((_, queries) => queries.GetTaxonomyChoices());

            _ = await Assert.That(choices.Categories.Single(choice => choice.Key == category).Name).IsEqualTo("IT Fresh Category");
            _ = await Assert.That(choices.Tags.Single(choice => choice.Key == tag).Kind).IsEqualTo(TagKinds.Style);
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, category, tag);
        }
    }

    [Test]
    public async Task GetCreateRecipeUrl_FindsTheWizardUnderTheListing()
    {
        var url = WithContent((content, queries) => queries.GetCreateRecipeUrl(
            (RecipeListingPage)content.GetById(TestContent.RecipeListing(Site.Services))!));

        _ = await Assert.That(url).IsEqualTo("/recipes/create-recipe/");
    }

    [Test]
    public async Task GetCategoryIcons_ReadsEachCategorysIcon()
    {
        var icons = WithContent((_, queries) => queries.GetCategoryIcons());

        _ = await Assert.That(string.Join(", ", icons.OrderBy(icon => icon.Key, StringComparer.Ordinal).Select(icon => $"{icon.Key} {icon.Value}")))
            .IsEqualTo("Beverage fa-duotone fa-mug-hot, Breakfast fa-duotone fa-egg, Dessert fa-duotone fa-cake-candles, Dinner fa-duotone fa-pot-food, Lunch fa-duotone fa-sandwich, Snack fa-duotone fa-cookie");
    }

    private T WithContent<T>(Func<IPublishedContentCache, IRecipeQueries, T> read)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        using var scope = Site.Services.CreateScope();
        return read(context.UmbracoContext.Content!, scope.ServiceProvider.GetRequiredService<IRecipeQueries>());
    }
}
