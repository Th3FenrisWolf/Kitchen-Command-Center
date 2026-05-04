using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using KCC.Admin.Modules.ResourceStrings;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsSectionPage),
    slug: "edit",
    uiPageType: typeof(ResourceStringsEditPage),
    name: "Edit Resource String",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
    IInfoProvider<ResourceStringTranslationInfo> translationProvider,
    ILocalizationService localizationService)
    : InfoEditPage<ResourceStringInfo>(formComponentMapper, formDataBinder)
{
    [PageParameter(typeof(IntPageModelBinder), typeof(ResourceStringsSectionPage))]
    public override int ObjectId { get; set; }

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        var items = await base.GetFormItems();

        ResourceStringFormHelper.RelabelValueField(items, contentLanguageProvider);

        var infoObject = await GetInfoObject();

        var existingTranslations = translationProvider.Get()
            .WhereEquals(
                nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID),
                infoObject.ResourceStringID)
            .ToList();

        ResourceStringFormHelper.AddTranslationFields(
            items,
            contentLanguageProvider,
            localizationService,
            existingTranslations);

        return items;
    }

    protected override async Task SetFormData(
        ResourceStringInfo infoObject,
        IFormFieldValueProvider fieldValueProvider)
    {
        await base.SetFormData(infoObject, fieldValueProvider);

        var existingTranslations = translationProvider.Get()
            .WhereEquals(
                nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID),
                infoObject.ResourceStringID)
            .ToList();

        ResourceStringFormHelper.SaveTranslations(
            infoObject.ResourceStringID,
            contentLanguageProvider,
            translationProvider,
            fieldName =>
            {
                fieldValueProvider.TryGet<string>(fieldName, out var value);
                return value;
            },
            existingTranslations);
    }
}
