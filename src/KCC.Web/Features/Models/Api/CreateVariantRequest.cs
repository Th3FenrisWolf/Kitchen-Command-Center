namespace KCC.Web.Features.Api;

public class CreateVariantRequest
{
    required public string VariantName { get; set; }
    required public string VariantDescription { get; set; }
    public int? PrepTime { get; set; }
    public int? CookTime { get; set; }
    public int? Servings { get; set; }
    public IEnumerable<IngredientDto> Ingredients { get; set; } = [];
    public IEnumerable<InstructionDto> Instructions { get; set; } = [];
}

public class IngredientDto
{
    required public string Name { get; set; }
    public decimal? Quantity { get; set; }
    required public string Unit { get; set; }
    public bool IsEyeballed { get; set; }
}

public class InstructionDto
{
    public int? Step { get; set; }
    required public string Text { get; set; }
}
