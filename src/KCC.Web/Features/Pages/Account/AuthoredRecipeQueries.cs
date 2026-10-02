using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Account;

public sealed record AuthoredRecipes(
    IReadOnlyList<AccountViewModel.AuthoredRecipeInput> Recipes,
    IReadOnlyList<AccountViewModel.AuthoredVariantInput> Variants,
    IReadOnlySet<Guid> PublishedRecipeKeys,
    IReadOnlySet<Guid> PublishedVariantKeys)
{
    public static AuthoredRecipes None { get; } = new([], [], new HashSet<Guid>(), new HashSet<Guid>());
}

// The account page lists a member's submissions before the owner publishes them, so this is the site's one read of
// saved, unpublished content. Published nodes come from the published cache; only drafts are loaded.
public class AuthoredRecipeQueries(
    IPublishedContentQuery contentQuery,
    IDocumentNavigationQueryService navigation,
    IContentService contentService)
{
    public AuthoredRecipes GetAuthoredBy(Guid memberKey)
    {
        var listing = contentQuery.ContentAtRoot().OfType<HomePage>().SelectMany(home => home.Children<RecipeListingPage>()).FirstOrDefault();
        if (listing is null || !navigation.TryGetChildrenKeysOfType(listing.Key, "recipe", out var recipeKeys))
        {
            return AuthoredRecipes.None;
        }

        var variantsByRecipe = recipeKeys.ToDictionary(
            recipeKey => recipeKey,
            recipeKey => navigation.TryGetChildrenKeysOfType(recipeKey, "recipeVariant", out var keys) ? keys.ToList() : []);
        var nodes = Describe(recipeKeys.Concat(variantsByRecipe.Values.SelectMany(keys => keys)).ToList());

        var variants = variantsByRecipe
            .SelectMany(pair => pair.Value.Select(variantKey => (RecipeKey: pair.Key, Node: nodes.GetValueOrDefault(variantKey))))
            .Where(variant => variant.Node?.AuthorKey == memberKey)
            .Select(variant => new AccountViewModel.AuthoredVariantInput(variant.Node.Key, variant.RecipeKey, variant.Node.Name, variant.Node.Icon, variant.Node.Url))
            .ToList();
        var parentKeys = variants.Select(variant => variant.ParentKey).ToHashSet();
        var recipes = recipeKeys
            .Select(recipeKey => nodes.GetValueOrDefault(recipeKey))
            .Where(recipe => recipe is not null && (recipe.AuthorKey == memberKey || parentKeys.Contains(recipe.Key)))
            .Select(recipe => new AccountViewModel.AuthoredRecipeInput(recipe.Key, recipe.Name, recipe.Icon, recipe.Url, recipe.AuthorKey == memberKey))
            .ToList();

        return new AuthoredRecipes(
            recipes,
            variants,
            recipes.Where(recipe => nodes[recipe.Key].IsPublished).Select(recipe => recipe.Key).ToHashSet(),
            variants.Where(variant => nodes[variant.Key].IsPublished).Select(variant => variant.Key).ToHashSet());
    }

    // A member picker stores its value as a member UDI, such as umb://member/0a1b…
    private static Guid? MemberKey(object value) =>
        value is string text && UdiParser.TryParse(text, out Udi udi) && udi is GuidUdi guidUdi ? guidUdi.Guid : null;

    private Dictionary<Guid, Node> Describe(IReadOnlyCollection<Guid> keys)
    {
        var nodes = new Dictionary<Guid, Node>();
        var drafts = new List<Guid>();
        foreach (var key in keys)
        {
            switch (contentQuery.Content(key))
            {
                case Recipe recipe:
                    nodes[key] = new Node(key, recipe.Name, recipe.Icon, recipe.Url(), recipe.Author?.Key, IsPublished: true);
                    break;
                case RecipeVariant variant:
                    nodes[key] = new Node(key, variant.Name, variant.Icon, variant.Url(), variant.Author?.Key, IsPublished: true);
                    break;
                default:
                    drafts.Add(key);
                    break;
            }
        }

        foreach (var draft in drafts.Count == 0 ? [] : contentService.GetByIds(drafts))
        {
            nodes[draft.Key] = new Node(draft.Key, draft.Name, draft.GetValue<string>("icon"), null, MemberKey(draft.GetValue("author")), IsPublished: false);
        }

        return nodes;
    }

    private sealed record Node(Guid Key, string Name, string Icon, string Url, Guid? AuthorKey, bool IsPublished);
}
