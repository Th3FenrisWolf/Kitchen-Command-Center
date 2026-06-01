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
public class RecipeIconSelectorFormComponent
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
