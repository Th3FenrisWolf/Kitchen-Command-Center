using System.Text.Json;
using System.Text.Json.Nodes;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Umbraco.Cms.Core.Models.ContentEditing;

namespace KCC.Web.Features.Submissions;

// Maps a member's wizard submission onto the recipe and variant properties. A value the member left out is left out
// here too: the draft then fails the type's mandatory check, which stops it being published until the owner fills it in.
public static class SubmissionValues
{
    public static List<PropertyValueModel> Recipe(CreateRecipeRequest request, string icon, Guid authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            Value("icon", icon),
            Value("author", authorKey.ToString()),
        };
        AddText(values, "description", request.RecipeDescription);
        if (request.Category is { } category)
        {
            values.Add(Value("category", DocumentReferences([category])));
        }

        AddText(values, "proposedCategory", request.ProposedCategory);
        return values;
    }

    public static List<PropertyValueModel> Variant(CreateVariantRequest request, string icon, Guid authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            Value("icon", icon),
            Value("author", authorKey.ToString()),
            Value("ingredients", IngredientsJson(request.Ingredients)),
            Value("instructions", InstructionsJson(request.Instructions)),
        };
        AddText(values, "description", request.VariantDescription);
        AddNumber(values, "prepTime", request.PrepTime);
        AddNumber(values, "cookTime", request.CookTime);
        AddNumber(values, "servings", request.Servings);
        AddDetails(values, request);
        return values;
    }

    // The stored shape the ingredients editor reads: an eyeballed row has no quantity and no unit.
    public static string IngredientsJson(IEnumerable<IngredientDto> ingredients) => JsonSerializer.Serialize(
        (ingredients ?? [])
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient?.Name))
            .Select(ingredient => new
            {
                name = ingredient.Name.Trim(),
                quantity = ingredient.IsEyeballed ? null : ingredient.Quantity,
                unit = ingredient.IsEyeballed ? string.Empty : ingredient.Unit?.Trim() ?? string.Empty,
                isEyeballed = ingredient.IsEyeballed,
            }),
        JsonNaming.CamelCase);

    public static string InstructionsJson(IEnumerable<InstructionDto> instructions) => JsonSerializer.Serialize(
        (instructions ?? [])
            .Where(instruction => !string.IsNullOrWhiteSpace(instruction?.Text))
            .Select((instruction, index) => new { step = index + 1, text = instruction.Text.Trim() }),
        JsonNaming.CamelCase);

    public static IEnumerable<string> IngredientNames(CreateVariantRequest request) =>
        (request?.Ingredients ?? []).Select(ingredient => ingredient?.Name).Where(name => !string.IsNullOrWhiteSpace(name));

    private static void AddDetails(List<PropertyValueModel> values, CreateVariantRequest request)
    {
        var tags = (request.Tags ?? []).Distinct().ToList();
        if (tags.Count > 0)
        {
            values.Add(Value("tags", DocumentReferences(tags)));
        }

        if (request.Difficulty is { } difficulty)
        {
            values.Add(Value("difficulty", new[] { difficulty }));
        }

        var nutrition = request.Nutrition ?? new NutritionDto();
        AddMeasure(values, "calories", nutrition.Calories);
        AddMeasure(values, "proteinG", nutrition.ProteinG);
        AddMeasure(values, "carbsG", nutrition.CarbsG);
        AddMeasure(values, "fatG", nutrition.FatG);
        AddMeasure(values, "saturatedFatG", nutrition.SaturatedFatG);
        AddMeasure(values, "fiberG", nutrition.FiberG);
        AddMeasure(values, "sugarG", nutrition.SugarG);
        AddMeasure(values, "sodiumMg", nutrition.SodiumMg);
        AddText(values, "proposedTags", request.ProposedTags);
    }

    // The multi-node tree picker's editor accepts only a JsonArray of { type, unique } references.
    private static JsonArray DocumentReferences(IEnumerable<Guid> keys) =>
        new(keys.Select(key => (JsonNode)new JsonObject { ["type"] = "document", ["unique"] = key.ToString() }).ToArray());

    private static PropertyValueModel Value(string alias, object value) => new() { Alias = alias, Value = value };

    private static void AddText(List<PropertyValueModel> values, string alias, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            values.Add(Value(alias, text.Trim()));
        }
    }

    private static void AddNumber(List<PropertyValueModel> values, string alias, int? number)
    {
        if (number is > 0)
        {
            values.Add(Value(alias, number.Value));
        }
    }

    // Unlike a time, a nutrition value of 0 is a real value the recipe page shows.
    private static void AddMeasure(List<PropertyValueModel> values, string alias, int? measure)
    {
        if (measure is { } given)
        {
            values.Add(Value(alias, given));
        }
    }
}
