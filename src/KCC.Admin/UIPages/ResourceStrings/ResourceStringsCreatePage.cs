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
    internal const string HiddenSubmitLabel = "__submit__";
    private const string AddAnotherFieldName = "AddAnother";
    private bool _addAnother;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();
        PageConfiguration.SubmitConfiguration.Label = HiddenSubmitLabel;
    }

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        var items = await base.GetFormItems();

        ResourceStringFormHelper.RelabelValueField(items, contentLanguageProvider);
        ResourceStringFormHelper.AddTranslationFields(items, contentLanguageProvider, localizationService);
        ResourceStringFormHelper.AddSaveAndAddAnotherField(items, AddAnotherFieldName);

        return items;
    }

    protected override async Task SetFormData(
        ResourceStringInfo infoObject,
        IFormFieldValueProvider fieldValueProvider)
    {
        fieldValueProvider.TryGet<bool>(AddAnotherFieldName, out var addAnother);
        _addAnother = addAnother;

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

    protected override Task<ICommandResponse> GetSubmitSuccessResponse(
        ResourceStringInfo savedInfoObject,
        ICollection<IFormItem> items)
    {
        if (_addAnother)
        {
            var url = pageLinkGenerator.GetPath<ResourceStringsCreatePage>();
            return Task.FromResult((ICommandResponse)NavigateTo(url)
                .AddSuccessMessage("Resource string saved."));
        }

        return base.GetSubmitSuccessResponse(savedInfoObject, items);
    }
}
