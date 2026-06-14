namespace KCC.Web.Features.Api;

/// <summary>
/// Request model for creating a recipe variant.
/// </summary>
public class CreateVariantRequest
{
    /// <summary>
    /// Gets or sets the variant name.
    /// </summary>
    required public string VariantName { get; set; }

    /// <summary>
    /// Gets or sets the variant description.
    /// </summary>
    required public string VariantDescription { get; set; }

    /// <summary>
    /// Gets or sets the preparation time in minutes.
    /// </summary>
    public int? PrepTime { get; set; }

    /// <summary>
    /// Gets or sets the cook time in minutes.
    /// </summary>
    public int? CookTime { get; set; }

    /// <summary>
    /// Gets or sets the number of servings.
    /// </summary>
    public int? Servings { get; set; }

    /// <summary>
    /// Gets or sets the list of ingredients.
    /// </summary>
    public IEnumerable<IngredientDto> Ingredients { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of instructions.
    /// </summary>
    public IEnumerable<InstructionDto> Instructions { get; set; } = [];
}

/// <summary>
/// Data transfer object for a single ingredient.
/// </summary>
public class IngredientDto
{
    /// <summary>
    /// Gets or sets the ingredient name.
    /// </summary>
    required public string Name { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement.
    /// </summary>
    required public string Unit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the quantity is eyeballed (approximate).
    /// </summary>
    public bool IsEyeballed { get; set; }
}

/// <summary>
/// Data transfer object for a single instruction step.
/// </summary>
public class InstructionDto
{
    /// <summary>
    /// Gets or sets the step number.
    /// </summary>
    public int? Step { get; set; }

    /// <summary>
    /// Gets or sets the instruction text.
    /// </summary>
    required public string Text { get; set; }
}
