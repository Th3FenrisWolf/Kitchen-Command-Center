#nullable enable

using CMS.ContentEngine;
using CMS.DataEngine;

namespace KCC.ResourceStrings.Editing;

public sealed record ContentLanguageOption(string Code, string Name);

public interface IContentLanguageRepository
{
    bool Exists(string code);

    IReadOnlyList<ContentLanguageOption> ListAll();
}

internal sealed class ContentLanguageRepository(IInfoProvider<ContentLanguageInfo> provider)
    : IContentLanguageRepository
{
    public bool Exists(string code) =>
        provider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), code)
            .TopN(1)
            .Any();

    public IReadOnlyList<ContentLanguageOption> ListAll() =>
        provider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName))
            .ToArray()
            .Select(l => new ContentLanguageOption(l.ContentLanguageName, l.ContentLanguageDisplayName))
            .ToList();
}
