using System.Reflection;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using KCC.ResourceStrings.Data;
using Kentico.Xperience.Admin.Base.Forms;

namespace KCC.ResourceStrings.Admin;

internal static class ResourceStringFormHelper
{
    internal const string TranslationPrefix = "translation_";

    private static void SetComponentName(IFormItem component, string name)
    {
        var nameProperty = component.GetType()
            .GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
        nameProperty?.SetValue(component, name);
    }

    public static void RelabelValueField(
        ICollection<IFormItem> items,
        IInfoProvider<ContentLanguageInfo> contentLanguageProvider)
    {
        var defaultLanguage = contentLanguageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .FirstOrDefault();

        if (defaultLanguage is null)
        {
            return;
        }

        var valueField = items.OfType<TextAreaComponent>()
            .FirstOrDefault(c => string.Equals(c.Name, nameof(ResourceStringInfo.ResourceStringValue), StringComparison.OrdinalIgnoreCase));

        if (valueField is not null)
        {
            valueField.Properties.Label = defaultLanguage.ContentLanguageDisplayName;
        }
    }

    public static void AddTranslationFields(
        ICollection<IFormItem> items,
        IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
        ILocalizationService localizationService,
        IList<ResourceStringTranslationInfo> existingTranslations = null)
    {
        var languages = contentLanguageProvider.Get()
            .WhereNotEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .ToList();

        foreach (var language in languages)
        {
            var textArea = new TextAreaComponent(localizationService);
            SetComponentName(textArea, $"{TranslationPrefix}{language.ContentLanguageID}");
            textArea.Properties.Label = language.ContentLanguageDisplayName;
            textArea.Properties.MinRowsNumber = 3;
            textArea.Properties.MaxRowsNumber = 5;

            var existing = existingTranslations?.FirstOrDefault(t =>
                t.ResourceStringTranslationContentLanguageID == language.ContentLanguageID);

            if (existing is not null && !string.IsNullOrEmpty(existing.ResourceStringTranslationValue))
            {
                textArea.SetValue(existing.ResourceStringTranslationValue);
            }

            items.Add(textArea);
        }
    }

    public static void AddSaveAndAddAnotherField(ICollection<IFormItem> items, string name, bool showAddAnother = true)
    {
        var component = new SaveAndAddAnotherComponent { ShowAddAnother = showAddAnother };
        SetComponentName(component, name);

        if (items is IList<IFormItem> list)
        {
            list.Insert(0, component);
        }
        else
        {
            items.Add(component);
        }
    }

    public static void SaveTranslations(
        int resourceStringId,
        IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
        IInfoProvider<ResourceStringTranslationInfo> translationProvider,
        Func<string, string> getTranslationValue,
        IList<ResourceStringTranslationInfo> existingTranslations = null)
    {
        var languages = contentLanguageProvider.Get()
            .WhereNotEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .ToList();

        foreach (var language in languages)
        {
            var translationValue = getTranslationValue(
                $"{TranslationPrefix}{language.ContentLanguageID}");

            var existing = existingTranslations?.FirstOrDefault(t =>
                t.ResourceStringTranslationContentLanguageID == language.ContentLanguageID);

            if (string.IsNullOrWhiteSpace(translationValue))
            {
                if (existing is not null)
                {
                    translationProvider.Delete(existing);
                }
            }
            else
            {
                if (existing is not null)
                {
                    existing.ResourceStringTranslationValue = translationValue;
                    translationProvider.Set(existing);
                }
                else
                {
                    var newTranslation = new ResourceStringTranslationInfo
                    {
                        ResourceStringTranslationResourceStringID = resourceStringId,
                        ResourceStringTranslationContentLanguageID = language.ContentLanguageID,
                        ResourceStringTranslationValue = translationValue,
                    };
                    translationProvider.Set(newTranslation);
                }
            }
        }
    }
}
