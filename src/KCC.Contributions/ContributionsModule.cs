using CMS;
using KCC.Contributions;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(ContributionsModule))]

namespace KCC.Contributions;

internal class ContributionsModule : AdminModule
{
    /// <remarks>
    /// Four places have to agree on these and none of them can check the others: the registration
    /// below, every <c>templateName</c> naming a component from this module, the
    /// <c>projectName</c> in <c>Client/webpack.config.js</c>, and the <c>AdminOrgName</c> property
    /// and <c>ProjectName</c> item metadata in the project file. Disagreement publishes the bundle
    /// under a path no template resolves, and the screen then renders an error boundary.
    /// </remarks>
    public const string OrgName = "kcc";

    public const string ProjectName = "contributions";

    public ContributionsModule() : base("KCC.Contributions")
    {
    }

    protected override void OnInit()
    {
        base.OnInit();
        RegisterClientModule(OrgName, ProjectName);
    }
}
