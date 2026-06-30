using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.VariantDetail;

public class VariantDetailViewModel : BasePageViewModel
{
    public string VariantName { get; set; }
    public string VariantDescription { get; set; }
    public string Icon { get; set; }
    public IEnumerable<ImageItem> Images { get; set; } = [];
    public int? PrepTime { get; set; }
    public int? CookTime { get; set; }
    public int? Servings { get; set; }
    public string Difficulty { get; set; }
    public int? Calories { get; set; }
    public int? ProteinG { get; set; }
    public int? CarbsG { get; set; }
    public int? FatG { get; set; }
    public int? FiberG { get; set; }
    public int? SugarG { get; set; }
    public int? SodiumMg { get; set; }
    public IEnumerable<string> Tags { get; set; } = [];
    public IEnumerable<IngredientViewModel> Ingredients { get; set; } = [];
    public IEnumerable<InstructionViewModel> Instructions { get; set; } = [];
    public string VariantSlug { get; set; }
    public string RecipeName { get; set; }
    public string RecipeSlug { get; set; }
    public string CreatedByName { get; set; }
    public IEnumerable<VariantBreadcrumb> Breadcrumbs { get; set; } = [];
    public IEnumerable<SiblingVariantViewModel> SiblingVariants { get; set; } = [];
}

public record VariantBreadcrumb(string LinkText, string Url);

public class IngredientViewModel
{
    public string Name { get; set; }
    public decimal? Quantity { get; set; }
    public string Unit { get; set; }
    public bool IsEyeballed { get; set; }
}

public class InstructionViewModel
{
    public int? Step { get; set; }
    public string Text { get; set; }
}

public class SiblingVariantViewModel
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
}
