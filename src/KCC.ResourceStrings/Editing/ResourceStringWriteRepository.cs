using CMS.ContentEngine;
using CMS.DataEngine;
using KCC.ResourceStrings.Data;

namespace KCC.ResourceStrings.Editing;

public interface IResourceStringWriteRepository
{
    void UpsertString(string key, string value);

    bool StringExists(string key);

    void UpsertTranslation(string key, string language, string value);
}

internal sealed class ResourceStringWriteRepository(
    IInfoProvider<ResourceStringInfo> stringProvider,
    IInfoProvider<ResourceStringTranslationInfo> translationProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider)
    : IResourceStringWriteRepository
{
    public void UpsertString(string key, string value)
    {
        var existing = stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .FirstOrDefault();

        if (existing is null)
        {
            existing = new ResourceStringInfo
            {
                ResourceStringKey = key,
                ResourceStringValue = value,
            };
        }
        else
        {
            existing.ResourceStringValue = value;
        }

        stringProvider.Set(existing);
    }

    public bool StringExists(string key) =>
        stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .Any();

    public void UpsertTranslation(string key, string language, string value)
    {
        var stringRow = stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .FirstOrDefault();
        if (stringRow is null)
        {
            return;
        }

        var languageId = languageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), language)
            .Column(nameof(ContentLanguageInfo.ContentLanguageID))
            .TopN(1)
            .GetScalarResult(0);
        if (languageId is 0)
        {
            return;
        }

        var existing = translationProvider.Get()
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID), stringRow.ResourceStringID)
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationContentLanguageID), languageId)
            .TopN(1)
            .FirstOrDefault();

        if (value is null)
        {
            if (existing is not null)
            {
                translationProvider.Delete(existing);
            }

            return;
        }

        if (existing is null)
        {
            existing = new ResourceStringTranslationInfo
            {
                ResourceStringTranslationResourceStringID = stringRow.ResourceStringID,
                ResourceStringTranslationContentLanguageID = languageId,
                ResourceStringTranslationValue = value,
            };
        }
        else
        {
            existing.ResourceStringTranslationValue = value;
        }

        translationProvider.Set(existing);
    }
}
