using CMS.ContentEngine;
using CMS.DataEngine;

namespace KCC.Admin.Models.Retrievers;

public static class DefaultLanguageRetriever
{
    private static readonly Lazy<string> DefaultLanguageName = new(() =>
        Provider<ContentLanguageInfo>.Instance.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .Column(nameof(ContentLanguageInfo.ContentLanguageName))
            .TopN(1)
            .GetScalarResult(string.Empty));

    private static readonly Lazy<int> DefaultLanguageId = new(() =>
        Provider<ContentLanguageInfo>.Instance.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .Column(nameof(ContentLanguageInfo.ContentLanguageID))
            .TopN(1)
            .GetScalarResult(0));

    public static string GetName() => DefaultLanguageName.Value;

    public static int GetId() => DefaultLanguageId.Value;
}
