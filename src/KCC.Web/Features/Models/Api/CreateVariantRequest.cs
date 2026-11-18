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
    public IEnumerable<Guid> Tags { get; set; } = [];
    public string Difficulty { get; set; }
    public NutritionDto Nutrition { get; set; }
    public string ProposedTags { get; set; }
}

public class NutritionDto
{
    public int? Calories { get; set; }
    public int? ProteinG { get; set; }
    public int? CarbsG { get; set; }
    public int? FatG { get; set; }
    public int? SaturatedFatG { get; set; }
    public int? FiberG { get; set; }
    public int? SugarG { get; set; }
    public int? SodiumMg { get; set; }
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
