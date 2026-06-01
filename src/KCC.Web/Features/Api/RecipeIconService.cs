using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using KCC.Admin;
using Microsoft.Extensions.Logging;

namespace KCC.Web.Features.Api;

/// <summary>
/// Picks the best curated Font Awesome icon for a recipe. Always returns a valid kebab-case
/// name from <see cref="RecipeIcons.All"/> — never empty.
/// </summary>
public interface IRecipeIconService
{
    /// <summary>Picks the best icon for a recipe.</summary>
    /// <param name="name">Recipe name.</param>
    /// <param name="description">Recipe description.</param>
    /// <param name="ingredients">Ingredient names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A kebab-case icon name from <see cref="RecipeIcons.All"/>.</returns>
    Task<string> PickAsync(string name, string description, IEnumerable<string> ingredients, CancellationToken cancellationToken);
}

/// <summary>Default <see cref="IRecipeIconService"/> backed by the Anthropic API with a deterministic fallback.</summary>
public class RecipeIconService(
    AnthropicClient client,
    AnthropicOptions options,
    ILogger<RecipeIconService> logger
) : IRecipeIconService
{
    private const string ToolName = "select_icon";

    private static readonly IReadOnlyDictionary<string, JsonElement> IconProperty =
        new Dictionary<string, JsonElement>
        {
            ["icon"] = JsonSerializer.SerializeToElement(new
            {
                type = "string",
                @enum = RecipeIcons.All,
            }),
        };

    /// <inheritdoc />
    public async Task<string> PickAsync(
        string name,
        string description,
        IEnumerable<string> ingredients,
        CancellationToken cancellationToken)
    {
        try
        {
            string ingredientList = string.Join(", ", ingredients ?? Array.Empty<string>());
            string userText =
                "Recipe name: " + name + "\n" +
                "Description: " + description + "\n" +
                "Ingredients: " + ingredientList + "\n\n" +
                "Call select_icon with the single icon that best represents this recipe.";

            var tool = new Tool
            {
                Name = ToolName,
                Description = "Select the one icon that best represents the recipe.",
                InputSchema = new InputSchema
                {
                    Type = JsonSerializer.SerializeToElement("object"),
                    Properties = IconProperty,
                    Required = new[] { "icon" },
                },
            };

            var parameters = new MessageCreateParams
            {
                Model = options.Model,
                MaxTokens = 128,
                System = "You assign a food/drink icon to a recipe. Always pick the closest match from the allowed list.",
                Tools = new ToolUnion[] { tool },
                ToolChoice = new ToolChoiceTool(ToolName),
                Messages = new[]
                {
                    new MessageParam
                    {
                        Role = Role.User,
                        Content = userText,
                    },
                },
            };

            var message = await client.Messages.Create(parameters);

            foreach (var block in message.Content)
            {
                if (block.TryPickToolUse(out var toolUse) && toolUse.Name == ToolName)
                {
                    string icon = ExtractIcon(toolUse.Input);
                    if (RecipeIcons.IsKnown(icon))
                    {
                        return icon;
                    }
                }
            }

            logger.LogWarning("Icon AI returned no valid icon for recipe {Recipe}; using fallback.", name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Icon AI call failed for recipe {Recipe}; using fallback.", name);
        }

        return RecipeIcons.Fallback(name);
    }

    private static string ExtractIcon(IReadOnlyDictionary<string, JsonElement> input)
    {
        if (input.TryGetValue("icon", out var iconElement))
        {
            return iconElement.GetString();
        }

        return null;
    }
}
