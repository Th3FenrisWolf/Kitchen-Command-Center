using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.RecipeImport;

namespace KCC.Web.Features.Pages.AddVariant;

public class AddVariantViewModel : BasePageViewModel
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; }

    public string RecipeSlug { get; set; }

    public ImportAvailabilityModel PhotoImport { get; set; }
}
