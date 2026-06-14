using System.Collections.Generic;
using System.Globalization;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account;

public class AccountViewModel : BasePageViewModel
{
    public string DisplayName { get; set; }
    public string Initials { get; set; }
    public string MemberSince { get; set; }

    public IEnumerable<RecipeGroupViewModel> RecipeGroups { get; set; } = [];

    public static string ComputeInitials(string firstName, string lastName, string fallback)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();

        var initials = string.Concat(
            first.Length > 0 ? first[..1] : string.Empty,
            last.Length > 0 ? last[..1] : string.Empty);

        if (initials.Length is 0)
        {
            var source = (fallback ?? string.Empty).Trim();
            initials = source.Length >= 2 ? source[..2] : source;
        }

        return initials.ToUpperInvariant();
    }

    public static string FormatMemberSince(DateTime? created) =>
        created?.ToString("MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>A recipe relevant to the member's profile: started by them, or parent of one of their variants.</summary>
    /// <param name="PageId">Web page item ID.</param>
    /// <param name="Name">Recipe display name.</param>
    /// <param name="Icon">Recipe icon class.</param>
    /// <param name="Url">Relative URL of the live page; only surfaced for published recipes.</param>
    /// <param name="StartedByMe">Whether the profile's member authored the recipe.</param>
    public record AuthoredRecipeInput(int PageId, string Name, string Icon, string Url, bool StartedByMe);

    /// <summary>A variant authored by the member.</summary>
    /// <param name="PageId">Web page item ID.</param>
    /// <param name="ParentPageId">Web page item ID of the parent recipe.</param>
    /// <param name="Name">Variant display name.</param>
    /// <param name="Icon">Variant icon class.</param>
    /// <param name="Url">Relative URL of the live page; only surfaced for published variants.</param>
    public record AuthoredVariantInput(int PageId, int ParentPageId, string Name, string Icon, string Url);

    /// <summary>
    /// Groups the member's variants under their recipes. Items absent from the published-id
    /// sets are pending review: badged, no URL. Recipes the member started always show;
    /// foreign recipes only show while they contain the member's variants. Variants whose
    /// parent recipe is missing (deleted) are skipped.
    /// </summary>
    /// <param name="recipes">Recipes the member started plus parents of their variants.</param>
    /// <param name="variants">Variants the member authored.</param>
    /// <param name="publishedRecipeIds">Page IDs of recipes that are published.</param>
    /// <param name="publishedVariantIds">Page IDs of variants that are published.</param>
    /// <returns>Ordered display groups for the profile's creations section.</returns>
    /// <remarks>Callers must supply unique <see cref="AuthoredRecipeInput.PageId"/> values; duplicates throw.</remarks>
    public static IEnumerable<RecipeGroupViewModel> BuildRecipeGroups(
        IReadOnlyCollection<AuthoredRecipeInput> recipes,
        IReadOnlyCollection<AuthoredVariantInput> variants,
        IReadOnlySet<int> publishedRecipeIds,
        IReadOnlySet<int> publishedVariantIds)
    {
        var groups = recipes.ToDictionary(
            recipe => recipe.PageId,
            recipe =>
            {
                var isPublished = publishedRecipeIds.Contains(recipe.PageId);
                return new RecipeGroupViewModel
                {
                    PageId = recipe.PageId,
                    RecipeName = recipe.Name,
                    RecipeIcon = recipe.Icon,
                    RecipeUrl = isPublished ? recipe.Url : null,
                    IsPending = !isPublished,
                    StartedByYou = recipe.StartedByMe,
                };
            });

        var variantsByParent = variants.ToLookup(variant => variant.ParentPageId);

        var enrichedGroups = groups.Values.Select(group =>
        {
            group.Variants = variantsByParent[group.PageId]
                .Select(variant =>
                {
                    var isPublished = publishedVariantIds.Contains(variant.PageId);
                    return new ProfileVariantViewModel
                    {
                        PageId = variant.PageId,
                        Name = variant.Name,
                        Icon = variant.Icon,
                        Url = isPublished ? variant.Url : null,
                        IsPending = !isPublished,
                    };
                })
                .OrderBy(variant => variant.Name, StringComparer.OrdinalIgnoreCase);

            return group;
        });

        return enrichedGroups
            .Where(group => group.StartedByYou || group.Variants.Any())
            .OrderBy(group => group.RecipeName, StringComparer.OrdinalIgnoreCase);
    }
}

public class RecipeGroupViewModel
{
    public int PageId { get; set; }
    public string RecipeName { get; set; }
    public string RecipeIcon { get; set; }
    public string RecipeUrl { get; set; }
    public bool IsPending { get; set; }
    public bool StartedByYou { get; set; }
    public IEnumerable<ProfileVariantViewModel> Variants { get; set; } = [];
}

public class ProfileVariantViewModel
{
    public int PageId { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }
    public bool IsPending { get; set; }
}
