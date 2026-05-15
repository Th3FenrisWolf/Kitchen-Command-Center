namespace KCC.ResourceStrings.Data;

public partial class ResourceStringInfo
{
    static ResourceStringInfo()
    {
        TYPEINFO.ContinuousIntegrationSettings.Enabled = true;
        TYPEINFO.SerializationSettings.ExcludedFieldNames.Add(nameof(ResourceStringValue));
    }

    public string Key => ResourceStringKey;

    public string Value => ResourceStringValue;
}
