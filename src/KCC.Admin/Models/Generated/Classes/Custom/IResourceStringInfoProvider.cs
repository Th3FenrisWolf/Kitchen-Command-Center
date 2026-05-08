namespace ResourceStrings;

public partial interface IResourceStringInfoProvider
{
    string GetOrDefault(string key);
    string GetOrDefault(string key, string languageName);
    Dictionary<string, string> GetOrDefault(params string[] keys);
}
