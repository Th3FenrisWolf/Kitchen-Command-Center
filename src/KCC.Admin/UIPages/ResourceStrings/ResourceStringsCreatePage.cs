using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using KCC.Admin.Modules.ResourceStrings;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using ResourceStrings;

[assembly: UIPage(
    parentType: typeof(ResourceStringsListingPage),
    slug: "create",
    uiPageType: typeof(ResourceStringsCreatePage),
    name: "Create Resource String",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
)]

namespace KCC.Admin.Modules.ResourceStrings;

public class ResourceStringsCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator,
    IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
    IInfoProvider<ResourceStringTranslationInfo> translationProvider,
    ILocalizationService localizationService)
    : CreatePage<ResourceStringInfo, ResourceStringsEditPage>(formComponentMapper, formDataBinder, pageLinkGenerator)
{
    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        var items = await base.GetFormItems();

        ResourceStringFormHelper.RelabelValueField(items, contentLanguageProvider);
        ResourceStringFormHelper.AddTranslationFields(items, contentLanguageProvider, localizationService);

        return items;
    }

    protected override async Task SetFormData(
        ResourceStringInfo infoObject,
        IFormFieldValueProvider fieldValueProvider)
    {
        await base.SetFormData(infoObject, fieldValueProvider);

        ResourceStringFormHelper.SaveTranslations(
            infoObject.ResourceStringID,
            contentLanguageProvider,
            translationProvider,
            fieldName =>
            {
                fieldValueProvider.TryGet<string>(fieldName, out var value);
                return value;
            });
    }
}
