namespace KCC.Web.Features.Api;

public class CreateRecipeRequest
{
    required public string RecipeName { get; set; }
    required public string RecipeDescription { get; set; }
    required public CreateVariantRequest FirstVariant { get; set; }
}
