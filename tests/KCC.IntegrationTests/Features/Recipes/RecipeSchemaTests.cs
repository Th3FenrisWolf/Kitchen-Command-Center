using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Recipes;

public class RecipeSchemaTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContentTypeService ContentTypes => Site.Services.GetRequiredService<IContentTypeService>();

    [Test]
    [Arguments("recipe")]
    [Arguments("recipeVariant")]
    public async Task RecipeType_ComposesMetadata(string alias)
    {
        var type = ContentTypes.Get(alias);

        _ = await Assert.That(type).IsNotNull();
        _ = await Assert.That(type!.ContentTypeComposition.Any(composed => composed.Alias == "metadata")).IsTrue();
    }

    [Test]
    [Arguments("recipe", "description,icon,image,category,author")]
    [Arguments("recipeVariant", "description,icon,images,difficulty,tags,author,prepTime,cookTime,servings,ingredients,instructions,calories,proteinG,carbsG,fatG,saturatedFatG,fiberG,sugarG,sodiumMg")]
    public async Task RecipeType_HasItsProperties(string alias, string properties)
    {
        var type = ContentTypes.Get(alias)!;

        var missing = properties.Split(',').Where(property => !type.PropertyTypeExists(property));

        _ = await Assert.That(string.Join(",", missing)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RecipeListing_AllowsRecipesAndListsThemAsACollection()
    {
        var listing = ContentTypes.Get("recipeListingPage")!;

        _ = await Assert.That(listing.AllowedContentTypes!.Any(allowed => allowed.Alias == "recipe")).IsTrue();
        _ = await Assert.That(listing.ListView).IsEqualTo(Constants.DataTypes.Guids.ListViewContentGuid);
    }

    [Test]
    public async Task Recipe_AllowsVariants()
    {
        var recipe = ContentTypes.Get("recipe")!;

        _ = await Assert.That(recipe.AllowedContentTypes!.Any(allowed => allowed.Alias == "recipeVariant")).IsTrue();
    }

    [Test]
    public async Task MemberType_HasFirstAndLastName()
    {
        var member = Site.Services.GetRequiredService<IMemberTypeService>().Get(Constants.Security.DefaultMemberTypeAlias)!;

        _ = await Assert.That(member.PropertyTypeExists("firstName")).IsTrue();
        _ = await Assert.That(member.PropertyTypeExists("lastName")).IsTrue();
    }
}
