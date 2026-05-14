using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;

namespace KCC.ResourceStrings;

public static class DefaultLanguageRetriever
{
    private const int CacheMinutes = 60;

    public static string GetName()
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                    $"{ContentLanguageInfo.OBJECT_TYPE}|all");

                return Provider<ContentLanguageInfo>.Instance.Get()
                    .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
                    .Column(nameof(ContentLanguageInfo.ContentLanguageName))
                    .TopN(1)
                    .GetScalarResult(string.Empty);
            },
            new CacheSettings(CacheMinutes, "KCC.DefaultLanguage", "Name"));
    }

    public static int GetId()
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                    $"{ContentLanguageInfo.OBJECT_TYPE}|all");

                return Provider<ContentLanguageInfo>.Instance.Get()
                    .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
                    .Column(nameof(ContentLanguageInfo.ContentLanguageID))
                    .TopN(1)
                    .GetScalarResult(0);
            },
            new CacheSettings(CacheMinutes, "KCC.DefaultLanguage", "Id"));
    }
}
