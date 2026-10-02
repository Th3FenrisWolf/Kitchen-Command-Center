using System.Globalization;
using Umbraco.Cms.Core.Dictionary;

namespace KCC.Web.Features.Dictionary;

public class DictionaryResourceStringProvider(ICultureDictionaryFactory dictionaryFactory) : IResourceStringProvider
{
    // The site has one language, and requests outside Umbraco's content routing (the API, /error) carry no culture
    // of their own, so every lookup names it.
    private static readonly CultureInfo SiteCulture = CultureInfo.GetCultureInfo("en-US");

    public string GetOrDefault(string key) => Resolve(dictionaryFactory.CreateDictionary(SiteCulture), key);

    public Dictionary<string, string> GetGroup(string parentKey) =>
        dictionaryFactory.CreateDictionary(SiteCulture).GetChildren(parentKey)
            .OrderBy(child => child.Key, StringComparer.Ordinal)
            .ToDictionary(child => child.Key, child => child.Value is { Length: > 0 } value ? value : child.Key, StringComparer.Ordinal);

    private static string Resolve(ICultureDictionary dictionary, string key) =>
        dictionary[key] is { Length: > 0 } value ? value : key;
}
