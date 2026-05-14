using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using KCC.Admin.Models.Retrievers;
using CacheConstants = KCC.Admin.Models.Constants.CacheConstants;

namespace ResourceStrings;

public partial class ResourceStringInfoProvider
{
    public static Func<string> LanguageRetriever { get; set; } = () => "";

    public string GetOrDefault(string key)
    {
        var languageName = LanguageRetriever.Invoke();

        var cache = Service.Resolve<IProgressiveCache>();

        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                [
                    $"{ResourceStringInfo.OBJECT_TYPE}|all",
                    $"{ResourceStringTranslationInfo.OBJECT_TYPE}|all",
                ]);

                var result = ResolveValues([key], languageName);
                return result.GetValueOrDefault(key, key);
            },
            new CacheSettings(CacheConstants.CacheMinutes, ResourceStringInfo.OBJECT_TYPE, languageName, key));
    }

    public Dictionary<string, string> GetOrDefault(params string[] keys)
    {
        var languageName = LanguageRetriever.Invoke();

        var cache = Service.Resolve<IProgressiveCache>();

        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                [
                    $"{ResourceStringInfo.OBJECT_TYPE}|all",
                    $"{ResourceStringTranslationInfo.OBJECT_TYPE}|all",
                ]);

                return ResolveValues(keys, languageName);
            },
            new CacheSettings(CacheConstants.CacheMinutes, ResourceStringInfo.OBJECT_TYPE, languageName, string.Join("|", keys)));
    }

    private Dictionary<string, string> ResolveValues(string[] keys, string languageName)
    {
        var resourceStrings = Get()
            .WhereIn(nameof(ResourceStringInfo.ResourceStringKey), keys)
            .ToArray();

        var resourceStringsByKey = resourceStrings.ToDictionary(r => r.ResourceStringKey);

        var languageId = ResolveLanguageId(languageName);
        var translationsByResourceId = GetTranslationsForStrings(resourceStrings, languageId);

        return keys.ToDictionary(
            key => key,
            key =>
            {
                if (!resourceStringsByKey.TryGetValue(key, out var resourceString))
                {
                    return key;
                }

                if (translationsByResourceId.TryGetValue(resourceString.ResourceStringID, out var translation))
                {
                    return translation;
                }

                return resourceString.ResourceStringValue ?? key;
            });
    }

    private static int ResolveLanguageId(string languageName) => Provider<ContentLanguageInfo>.Instance.Get()
        .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), languageName)
        .Column(nameof(ContentLanguageInfo.ContentLanguageID))
        .TopN(1)
        .GetScalarResult(0);

    private static Dictionary<int, string> GetTranslationsForStrings(ResourceStringInfo[] resourceStrings, int languageId)
    {
        if (languageId is 0 || resourceStrings.Length is 0)
        {
            return [];
        }

        var defaultLanguageId = DefaultLanguageRetriever.GetId();
        var resourceIds = resourceStrings.Select(r => r.ResourceStringID);

        var currentLanguageId = languageId;
        var visitedLanguageIds = new HashSet<int>();
        var result = new Dictionary<int, string>();

        while (currentLanguageId is not 0 && currentLanguageId != defaultLanguageId && visitedLanguageIds.Add(currentLanguageId))
        {
            var unresolvedIds = resourceIds.Except(result.Keys).ToArray();

            if (unresolvedIds.Length is 0)
            {
                break;
            }

            var translations = Provider<ResourceStringTranslationInfo>.Instance.Get()
                .WhereIn(nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID), unresolvedIds)
                .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationContentLanguageID), currentLanguageId)
                .ToArray();

            foreach (var translation in translations)
            {
                result[translation.ResourceStringTranslationResourceStringID] = translation.ResourceStringTranslationValue;
            }

            currentLanguageId = Provider<ContentLanguageInfo>.Instance.Get()
                .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageID), currentLanguageId)
                .Column(nameof(ContentLanguageInfo.ContentLanguageFallbackContentLanguageID))
                .TopN(1)
                .GetScalarResult(0);
        }

        return result;
    }
}
