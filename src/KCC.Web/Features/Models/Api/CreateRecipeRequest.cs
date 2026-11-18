namespace KCC.Web.Features.Api;

public class CreateRecipeRequest
{
    required public string RecipeName { get; set; }
    required public string RecipeDescription { get; set; }
    public Guid? Category { get; set; }
    public string ProposedCategory { get; set; }
    required public CreateVariantRequest FirstVariant { get; set; }
}
