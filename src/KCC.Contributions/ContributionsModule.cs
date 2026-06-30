using CMS;
using KCC.Contributions;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(ContributionsModule))]

namespace KCC.Contributions;

internal class ContributionsModule : AdminModule
{
    public ContributionsModule()
        : base("KCC.Contributions")
    {
    }
}
