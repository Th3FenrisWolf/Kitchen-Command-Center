using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    "KCC.SaveAndAddAnother",
    typeof(KCC.Admin.Modules.ResourceStrings.SaveAndAddAnotherComponent),
    "Save and Add Another")]

namespace KCC.Admin.Modules.ResourceStrings;

public class SaveAndAddAnotherClientProperties : FormComponentClientProperties<bool>
{
    public bool ShowAddAnother { get; set; } = true;
}

public class SaveAndAddAnotherComponent : FormComponent<SaveAndAddAnotherClientProperties, bool>
{
    public override string ClientComponentName => "@kcc/admin/SaveAndAddAnother";

    public bool ShowAddAnother { get; set; } = true;

    protected override Task ConfigureClientProperties(SaveAndAddAnotherClientProperties clientProperties)
    {
        clientProperties.ShowAddAnother = ShowAddAnother;
        return base.ConfigureClientProperties(clientProperties);
    }
}
