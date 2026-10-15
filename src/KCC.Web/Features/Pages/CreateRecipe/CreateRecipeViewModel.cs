using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.RecipeImport;

namespace KCC.Web.Features.Pages.CreateRecipe;

public class CreateRecipeViewModel : BasePageViewModel
{
    public string RecipesUrl { get; set; }

    public ImportAvailabilityModel PhotoImport { get; set; }
}
