namespace ResourceStrings;

public partial interface IResourceStringInfoProvider
{
    string GetOrDefault(string key);
    Dictionary<string, string> GetOrDefault(params string[] keys);
}
