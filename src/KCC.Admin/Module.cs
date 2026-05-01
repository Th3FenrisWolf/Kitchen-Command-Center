using CMS;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(KCC.Admin.KccAdminModule))]

namespace KCC.Admin
{
    internal class KccAdminModule : AdminModule
    {
        public KccAdminModule()
            : base("KCC.Admin")
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Registers the embedded client module (kcc/admin) so its templates
            // become available under @kcc/admin/<TemplateName>.
            RegisterClientModule("kcc", "admin");
        }
    }
}
