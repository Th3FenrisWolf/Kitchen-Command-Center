using CMS;
using KCC.ResourceStrings;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(ResourceStringsModule))]

namespace KCC.ResourceStrings;

internal class ResourceStringsModule : AdminModule
{
    public ResourceStringsModule() : base("KCC.ResourceStrings") { }

    protected override void OnInit()
    {
        base.OnInit();
        RegisterClientModule("kcc", "resource-strings");
    }
}
