using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Admin.FormComponents;
using KCC.Admin.FormComponents.IngredientsEditor;

[assembly: RegisterFormComponent(
    IngredientsEditorFormComponent.Identifier,
    typeof(IngredientsEditorFormComponent),
    "Recipe ingredients editor")]

namespace KCC.Admin.FormComponents.IngredientsEditor;

/// <summary>
/// Admin form component that edits the Recipe Variant <c>Ingredients</c> field — a JSON array of
/// ingredients — with a structured, reorderable editor. Stores the JSON string unchanged.
/// </summary>
[ComponentAttribute(typeof(IngredientsEditorComponentAttribute))]
public class IngredientsEditorFormComponent : FormComponent<IngredientsEditorClientProperties, string>
{
    /// <summary>Identifier referenced from the content-type field definition's controlname.</summary>
    public const string Identifier = "KCC.IngredientsEditor";

    /// <inheritdoc />
    public override string ClientComponentName => "@kcc/admin/IngredientsEditor";

    /// <inheritdoc />
    protected override Task ConfigureClientProperties(IngredientsEditorClientProperties clientProperties)
    {
        clientProperties.Units = [.. RecipeUnits.All];
        return base.ConfigureClientProperties(clientProperties);
    }
}

/// <summary>Editing-component attribute for <see cref="IngredientsEditorFormComponent"/>.</summary>
public class IngredientsEditorComponentAttribute : FormComponentAttribute
{
}

/// <summary>Client properties for the ingredients editor: the curated unit list for the datalist.</summary>
public class IngredientsEditorClientProperties : FormComponentClientProperties<string>
{
    /// <summary>Standard measurement units offered in the unit combo box.</summary>
    public string[] Units { get; set; } = [];
}
