using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using KCC.Admin.FormComponents.InstructionsEditor;

[assembly: RegisterFormComponent(
    InstructionsEditorFormComponent.Identifier,
    typeof(InstructionsEditorFormComponent),
    "Recipe instructions editor")]

namespace KCC.Admin.FormComponents.InstructionsEditor;

/// <summary>
/// Admin form component that edits the Recipe Variant <c>Instructions</c> field — a JSON array of
/// numbered steps — with a structured, reorderable editor. Stores the JSON string unchanged.
/// </summary>
[ComponentAttribute(typeof(InstructionsEditorComponentAttribute))]
public class InstructionsEditorFormComponent : FormComponent<InstructionsEditorClientProperties, string>
{
    /// <summary>Identifier referenced from the content-type field definition's controlname.</summary>
    public const string Identifier = "KCC.InstructionsEditor";

    /// <inheritdoc />
    public override string ClientComponentName => "@kcc/admin/InstructionsEditor";
}

/// <summary>Editing-component attribute for <see cref="InstructionsEditorFormComponent"/>.</summary>
public class InstructionsEditorComponentAttribute : FormComponentAttribute
{
}

/// <summary>Client properties for the instructions editor (none beyond the base value).</summary>
public class InstructionsEditorClientProperties : FormComponentClientProperties<string>
{
}
