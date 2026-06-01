namespace KCC.Web.Features.Api;

/// <summary>
/// Request model for creating a new recipe with an initial variant.
/// </summary>
public class CreateRecipeRequest
{
    /// <summary>
    /// Gets or sets the recipe name.
    /// </summary>
    required public string RecipeName { get; set; }

    /// <summary>
    /// Gets or sets the recipe description.
    /// </summary>
    required public string RecipeDescription { get; set; }

    /// <summary>
    /// Gets or sets the first variant to create alongside the recipe.
    /// </summary>
    required public CreateVariantRequest FirstVariant { get; set; }
}
