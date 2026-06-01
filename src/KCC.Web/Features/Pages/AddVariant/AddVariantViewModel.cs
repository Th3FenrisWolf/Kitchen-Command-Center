using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.AddVariant;

public class AddVariantViewModel : BasePageViewModel
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; }
    public string RecipeSlug { get; set; }
}
