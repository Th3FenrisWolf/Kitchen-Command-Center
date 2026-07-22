using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeSearchViewModel : BasePageViewModel
{
    /// <summary>Gets or sets the unfiltered first page, in the envelope shape shared with /api/recipes/search.</summary>
    public object InitialResults { get; set; }

    public string CreateRecipeUrl { get; set; }

    public IEnumerable<RecipeSearchBreadcrumb> Breadcrumbs { get; set; } = [];
}

public record RecipeSearchBreadcrumb(string LinkText, string Url);
