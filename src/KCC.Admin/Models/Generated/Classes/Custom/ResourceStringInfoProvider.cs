#nullable enable

using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using KCC.Admin.Models.Retrievers;
using CacheConstants = KCC.Admin.Models.Constants.CacheConstants;

namespace ResourceStrings;

public partial class ResourceStringInfoProvider
{
    public static Func<string?>? LanguageRetriever { get; set; }

    public string GetOrDefault(string key, string? languageName = null)
    {
        languageName ??= LanguageRetriever?.Invoke();

        var cache = Service.Resolve<IProgressiveCache>();

        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                [
                    $"{ResourceStringInfo.OBJECT_TYPE}|all",
                    $"{ResourceStringTranslationInfo.OBJECT_TYPE}|all",
                ]);

                return ResolveValue(key, languageName);
            },
            new CacheSettings(CacheConstants.CacheMinutes, ResourceStringInfo.OBJECT_TYPE, key, languageName ?? string.Empty));
    }

    private string ResolveValue(string key, string? languageName)
    {
        var resourceString = Get(key);

        if (resourceString is null)
        {
            return key;
        }

        if (string.IsNullOrEmpty(languageName))
        {
            return resourceString.Value ?? key;
        }

        var language = Provider<ContentLanguageInfo>.Instance.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), languageName)
            .TopN(1)
            .FirstOrDefault();

        if (language is null)
        {
            return resourceString.Value ?? key;
        }

        var defaultLanguageId = DefaultLanguageRetriever.GetId();
        var currentLanguageId = language.ContentLanguageID;
        var visitedLanguageIds = new HashSet<int>();

        while (currentLanguageId != 0 && currentLanguageId != defaultLanguageId && visitedLanguageIds.Add(currentLanguageId))
        {
            var translation = Provider<ResourceStringTranslationInfo>.Instance.Get()
                .WhereEquals(
                    nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID),
                    resourceString.ResourceStringID)
                .WhereEquals(
                    nameof(ResourceStringTranslationInfo.ResourceStringTranslationContentLanguageID),
                    currentLanguageId)
                .TopN(1)
                .FirstOrDefault();

            if (translation is not null)
            {
                return translation.ResourceStringTranslationValue;
            }

            var fallbackLanguageId = Provider<ContentLanguageInfo>.Instance.Get()
                .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageID), currentLanguageId)
                .Column(nameof(ContentLanguageInfo.ContentLanguageFallbackContentLanguageID))
                .TopN(1)
                .GetScalarResult(0);

            currentLanguageId = fallbackLanguageId;
        }

        return resourceString.Value ?? key;
    }
}
