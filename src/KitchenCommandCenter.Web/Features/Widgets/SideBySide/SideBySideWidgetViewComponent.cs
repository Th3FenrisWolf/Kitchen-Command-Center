using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Features.Cache;
using KitchenCommandCenter.Web.Features.Widgets.SideBySide;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: SideBySideWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(SideBySideWidgetViewComponent),
    name: "Side by side",
    propertiesType: typeof(SideBySideWidgetProperties),
    IconClass = "icon-two-rectangles-v",
    AllowCache = true
)]

namespace KitchenCommandCenter.Web.Features.Widgets.SideBySide;

public class SideBySideWidgetViewComponent(
    IPageBuilderComponentPropertiesRetriever componentPropertiesRetriever,
    ICacheService cacheService
) : ViewComponent
{
    public const string IDENTIFIER = "KitchenCommandCenter.Web.SideBySide";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var properties = componentPropertiesRetriever.Retrieve<SideBySideWidgetProperties>();

        var itemGuids = properties.SideBySideItems?.Select(item => item.Identifier);

        if (!itemGuids?.Any() ?? true)
        {
            return View(
                "~/Features/Widgets/SideBySide/_SideBySide.cshtml",
                new SideBySideWidgetViewModel()
            );
        }

        var items = (await GetItemData(itemGuids)).OrderBy(item =>
            itemGuids.ToList().IndexOf(item.SystemFields.ContentItemGUID)
        );

        var viewModel = new SideBySideWidgetViewModel
        {
            Items = GetSideBySideItems(items),
            Layout = properties.FirstElementLayout,
        };

        return View("~/Features/Widgets/SideBySide/_SideBySide.cshtml", viewModel);
    }

    private static IEnumerable<SideBySideItem> GetSideBySideItems(
        IEnumerable<IContentItemFieldsSource> queryItems
    )
    {
        foreach (var item in queryItems)
        {
            if (item is PageBuilderPage page)
            {
                yield return page.MapToSideBySideItem();
            }

            if (item is ImageItem imageItem)
            {
                yield return imageItem.MapToSideBySideItem();
            }
        }
    }

    private async Task<IEnumerable<IContentItemFieldsSource>> GetItemData(
        IEnumerable<Guid> itemGuids
    )
    {
        var contentQuery = new ContentItemQueryBuilder()
            .ForContentTypes(config =>
                config
                    .WithContentTypeFields()
                    .WithWebPageData()
                    .OfContentType([.. GetItemContentTypes()])
                    .WithLinkedItems(1)
            )
            .Parameters(parameters =>
                parameters.Where(where =>
                    where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), itemGuids)
                )
            );

        return await cacheService.Get<IContentItemFieldsSource>(
            contentQuery,
            [
                nameof(SideBySideWidgetViewComponent),
                nameof(InvokeAsync),
                .. itemGuids.Select(item => item.ToString()),
            ]
        );
    }

    private static IEnumerable<string> GetItemContentTypes()
    {
        var itemProperties = typeof(SideBySideWidgetProperties).GetProperty(
            nameof(SideBySideWidgetProperties.SideBySideItems)
        );

        return itemProperties
            .GetCustomAttributes(true)
            .Select(static attr => attr as ContentItemSelectorComponentAttribute)
            .Where(static attr => attr != null)
            .SelectMany(static values => values?.AllowedContentItemTypeIdentifiers)
            .Select(static guid =>
                DataClassInfoProvider
                    .GetClasses()
                    .WhereEquals(nameof(DataClassInfo.ClassGUID), guid)
                    .TopN(1)
                    .FirstOrDefault()
                    ?.ClassName
            );
    }
}
