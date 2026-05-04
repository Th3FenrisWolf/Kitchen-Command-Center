#nullable enable

namespace ResourceStrings;

public partial interface IResourceStringInfoProvider
{
    string GetOrDefault(string key, string? languageName = null);
}
