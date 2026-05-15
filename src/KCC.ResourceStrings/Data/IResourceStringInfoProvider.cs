namespace KCC.ResourceStrings.Data;

public partial interface IResourceStringInfoProvider
{
    string GetOrDefault(string key);
    string GetOrDefault(string key, string languageName);
    Dictionary<string, string> GetManyOrDefault(params string[] keys);
}
