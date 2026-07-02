namespace KCC.Contributions.Data;

public partial class VariantCookedInfo
{
    static VariantCookedInfo()
    {
        // Community-contributed runtime data — excluded from Continuous Integration.
        // This type has no GUID or code-name column, so CI cannot serialize it (ci-restore
        // throws NotSupportedException in RepositoryPathHelper.GetFileName if enabled).
        TYPEINFO.ContinuousIntegrationSettings.Enabled = false;
    }
}
