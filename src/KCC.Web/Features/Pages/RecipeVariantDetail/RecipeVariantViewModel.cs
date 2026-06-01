using System.Text.Json;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.RecipeVariantDetail;

public class RecipeVariantViewModel : BasePageViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string VariantName { get; set; }
    public string VariantDescription { get; set; }
    public List<string> ImagePaths { get; set; } = [];
    public int? PrepTime { get; set; }
    public int? CookTime { get; set; }
    public int? Servings { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<IngredientViewModel> Ingredients { get; set; } = [];
    public List<InstructionViewModel> Instructions { get; set; } = [];
    public string VariantSlug { get; set; }
    public string RecipeName { get; set; }
    public string RecipeSlug { get; set; }
    public List<SiblingVariantViewModel> SiblingVariants { get; set; } = [];

    public static List<IngredientViewModel> DeserializeIngredients(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<IngredientViewModel>>(json, JsonOptions) ?? [];
    }

    public static List<InstructionViewModel> DeserializeInstructions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<InstructionViewModel>>(json, JsonOptions) ?? [];
    }
}

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
}
