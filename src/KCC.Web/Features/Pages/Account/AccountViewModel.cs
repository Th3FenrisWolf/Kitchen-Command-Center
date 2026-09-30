using System.Globalization;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account;

public class AccountViewModel : BasePageViewModel
{
    public string DisplayName { get; set; }

    public string Initials { get; set; }

    public string MemberSince { get; set; }

    public string SettingsUrl { get; set; }

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

    public static IEnumerable<RecipeGroupViewModel> BuildRecipeGroups(
        IReadOnlyCollection<AuthoredRecipeInput> recipes,
        IReadOnlyCollection<AuthoredVariantInput> variants,
        IReadOnlySet<Guid> publishedRecipeKeys,
        IReadOnlySet<Guid> publishedVariantKeys)
    {
        var groups = recipes.ToDictionary(
            recipe => recipe.Key,
            recipe =>
            {
                var isPublished = publishedRecipeKeys.Contains(recipe.Key);
                return new RecipeGroupViewModel
                {
                    Key = recipe.Key,
                    RecipeName = recipe.Name,
                    RecipeIcon = recipe.Icon,
                    RecipeUrl = isPublished ? recipe.Url : null,
                    IsPending = !isPublished,
                    StartedByYou = recipe.StartedByMe,
                };
            });

        var variantsByParent = variants.ToLookup(variant => variant.ParentKey);

        var enrichedGroups = groups.Values.Select(group =>
        {
            group.Variants = variantsByParent[group.Key]
                .Select(variant =>
                {
                    var isPublished = publishedVariantKeys.Contains(variant.Key);
                    return new ProfileVariantViewModel
                    {
                        Key = variant.Key,
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

    public record AuthoredRecipeInput(Guid Key, string Name, string Icon, string Url, bool StartedByMe);

    public record AuthoredVariantInput(Guid Key, Guid ParentKey, string Name, string Icon, string Url);
}

public class RecipeGroupViewModel
{
    public Guid Key { get; set; }

    public string RecipeName { get; set; }

    public string RecipeIcon { get; set; }

    public string RecipeUrl { get; set; }

    public bool IsPending { get; set; }

    public bool StartedByYou { get; set; }

    public IEnumerable<ProfileVariantViewModel> Variants { get; set; } = [];
}

public class ProfileVariantViewModel
{
    public Guid Key { get; set; }

    public string Name { get; set; }

    public string Icon { get; set; }

    public string Url { get; set; }

    public bool IsPending { get; set; }
}
