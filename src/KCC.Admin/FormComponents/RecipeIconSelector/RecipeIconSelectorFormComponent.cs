using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Admin;
using KCC.Admin.FormComponents.RecipeIconSelector;

[assembly: RegisterFormComponent(
    RecipeIconSelectorFormComponent.Identifier,
    typeof(RecipeIconSelectorFormComponent),
    "Recipe icon selector")]

namespace KCC.Admin.FormComponents.RecipeIconSelector;

/// <summary>
/// Admin form component that lets editors pick a recipe icon visually from the curated
/// <see cref="RecipeIcons.All"/> set. Stores the selected full Font Awesome class string.
/// </summary>
[ComponentAttribute(typeof(RecipeIconSelectorComponentAttribute))]
public class RecipeIconSelectorFormComponent(IRecipeIconService iconService)
    : FormComponent<RecipeIconSelectorClientProperties, string>
{
    /// <summary>Identifier referenced from the content-type field definition's controlname.</summary>
    public const string Identifier = "KCC.RecipeIconSelector";

    /// <inheritdoc />
    public override string ClientComponentName => "@kcc/admin/RecipeIconSelector";

    /// <inheritdoc />
    protected override Task ConfigureClientProperties(RecipeIconSelectorClientProperties clientProperties)
    {
        clientProperties.Icons = [.. RecipeIcons.All];
        return base.ConfigureClientProperties(clientProperties);
    }

    /// <summary>
    /// Picks a curated icon from the recipe's current name/description and returns it to the client,
    /// which applies it via <c>onChange</c> — giving an instant preview without a page reload.
    /// Always returns a valid icon (the service falls back deterministically).
    /// </summary>
    [FormComponentCommand]
    public async Task<ICommandResponse<SuggestIconResponse>> SuggestIcon(SuggestIconArguments args)
    {
        string icon = await iconService.PickAsync(
            args?.Name ?? string.Empty,
            args?.Description ?? string.Empty,
            [],
            CancellationToken.None);

        return ResponseFrom(new SuggestIconResponse { Icon = icon });
    }
}

/// <summary>Editing-component attribute for <see cref="RecipeIconSelectorFormComponent"/>.</summary>
public class RecipeIconSelectorComponentAttribute : FormComponentAttribute
{
}

/// <summary>Client properties sent to the React selector: the curated icon class strings.</summary>
public class RecipeIconSelectorClientProperties : FormComponentClientProperties<string>
{
    /// <summary>The curated Font Awesome class strings the picker offers.</summary>
    public string[] Icons { get; set; } = [];
}

/// <summary>Arguments for the <c>SuggestIcon</c> command — the recipe context the AI picks from.</summary>
public class SuggestIconArguments
{
    /// <summary>Current recipe name from the editing form.</summary>
    public string Name { get; set; }

    /// <summary>Current recipe description from the editing form.</summary>
    public string Description { get; set; }
}

/// <summary>Response for the <c>SuggestIcon</c> command — the chosen full Font Awesome class string.</summary>
public class SuggestIconResponse
{
    /// <summary>The full Font Awesome class string for the suggested icon.</summary>
    public string Icon { get; set; }
}
