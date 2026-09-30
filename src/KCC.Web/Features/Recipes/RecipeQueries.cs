using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Recipes;

public interface IRecipeQueries
{
    RecipePageData GetRecipePage(Recipe recipe);

    VariantPageData GetVariantPage(RecipeVariant variant);
}

public class RecipeQueries : IRecipeQueries
{
    public RecipePageData GetRecipePage(Recipe recipe) => new(
        RecipeFrom(recipe),
        recipe.Children<RecipeVariant>().Select(VariantFrom).ToList(),
        recipe.Parent<RecipeListingPage>()?.Children<AddVariantPage>().FirstOrDefault()?.Url());

    public VariantPageData GetVariantPage(RecipeVariant variant)
    {
        var recipe = variant.Parent<Recipe>();
        if (recipe is null)
        {
            return null;
        }

        return new(
            VariantFrom(variant),
            RecipeFrom(recipe),
            recipe.Children<RecipeVariant>().Where(sibling => sibling.Key != variant.Key).Select(VariantFrom).ToList());
    }

    private static RecipeRecord RecipeFrom(Recipe recipe) => new(
        recipe.Key,
        recipe.Name,
        recipe.Url(),
        recipe.Description,
        recipe.Icon,
        RecipeImages.TileUrl(recipe.Image),
        recipe.Category?.Name,
        recipe.Author?.Key,
        recipe.CreateDate);

    private static VariantRecord VariantFrom(RecipeVariant variant) => new(
        variant.Key,
        variant.Name,
        variant.Url(),
        variant.Description,
        variant.Icon,
        RecipeImages.TileUrl(variant.Images?.FirstOrDefault()),
        variant.PrepTime,
        variant.CookTime,
        variant.Servings,
        string.IsNullOrEmpty(variant.Difficulty) ? null : variant.Difficulty.ToLowerInvariant(),
        new NutritionRecord(
            Optional(variant, "calories", variant.Calories),
            Optional(variant, "proteinG", variant.ProteinG),
            Optional(variant, "carbsG", variant.CarbsG),
            Optional(variant, "fatG", variant.FatG),
            Optional(variant, "saturatedFatG", variant.SaturatedFatG),
            Optional(variant, "fiberG", variant.FiberG),
            Optional(variant, "sugarG", variant.SugarG),
            Optional(variant, "sodiumMg", variant.SodiumMg)),
        (variant.Tags ?? []).Select(tag => tag.Name).ToList(),
        variant.Ingredients,
        variant.Instructions,
        variant.Author?.Key,
        variant.CreateDate);

    // An empty integer property reads as 0, which would print a real zero where the owner entered nothing.
    private static int? Optional(IPublishedContent content, string alias, int value) =>
        content.GetProperty(alias)?.HasValue() == true ? value : null;
}
