using Kentico.Xperience.Admin.Base.Forms;
using KCC.ResourceStrings.Admin;

[assembly: RegisterFormComponent(
    "KCC.SaveAndAddAnother",
    typeof(SaveAndAddAnotherComponent),
    "Save and Add Another")]

namespace KCC.ResourceStrings.Admin;

public class SaveAndAddAnotherClientProperties : FormComponentClientProperties<bool>
{
    public bool ShowAddAnother { get; set; } = true;
}

public class SaveAndAddAnotherComponent : FormComponent<SaveAndAddAnotherClientProperties, bool>
{
    public override string ClientComponentName => "@kcc/resource-strings/SaveAndAddAnother";

    public bool ShowAddAnother { get; set; } = true;

    protected override Task ConfigureClientProperties(SaveAndAddAnotherClientProperties clientProperties)
    {
        clientProperties.ShowAddAnother = ShowAddAnother;
        return base.ConfigureClientProperties(clientProperties);
    }
}
