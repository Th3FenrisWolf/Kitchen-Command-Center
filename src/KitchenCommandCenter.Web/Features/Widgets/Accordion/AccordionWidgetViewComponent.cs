using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using KitchenCommandCenter.Web.Features.Cache;
using KitchenCommandCenter.Web.Features.Widgets.Accordion;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: AccordionWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(AccordionWidgetViewComponent),
    name: "Accordion",
    propertiesType: typeof(AccordionWidgetProperties),
    IconClass = "icon-accordion",
    AllowCache = true
)]

namespace KitchenCommandCenter.Web.Features.Widgets.Accordion;

public class AccordionWidgetViewComponent(
    IPageBuilderComponentPropertiesRetriever componentPropertiesRetriever,
    ICacheService cacheService,
    IMapper mapper
) : ViewComponent
{
    public const string IDENTIFIER = "KitchenCommandCenter.Web.Accordion";
    public const string VIEWPATH = "~/Features/Widgets/Accordion/_AccordionWidget.cshtml";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var properties = componentPropertiesRetriever.Retrieve<AccordionWidgetProperties>();
        var accordionIdentifiers = properties.AccordionItems?.Select(item => item.Identifier);

        if (!(accordionIdentifiers?.Any() ?? false))
        {
            return View(VIEWPATH, new AccordionWidgetViewModel());
        }

        var accordionItemsQuery = new ContentItemQueryBuilder().ForContentType(
            KitchenCommandCenter.AccordionItem.CONTENT_TYPE_NAME,
            config =>
                config.Where(where =>
                    where.WhereIn(
                        nameof(IContentQueryDataContainer.ContentItemGUID),
                        accordionIdentifiers
                    )
                )
        );

        var accordionItems = (
            await cacheService.Get<KitchenCommandCenter.AccordionItem>(
                accordionItemsQuery,
                [
                    nameof(AccordionWidgetViewComponent),
                    nameof(InvokeAsync),
                    string.Join(',', accordionIdentifiers),
                ]
            )
        ).OrderBy(item => accordionIdentifiers.ToList().IndexOf(item.SystemFields.ContentItemGUID));

        var viewModel = new AccordionWidgetViewModel
        {
            Items = mapper.Map<IEnumerable<AccordionItem>>(accordionItems),
            Properties = properties,
        };

        return View(VIEWPATH, viewModel);
    }
}
