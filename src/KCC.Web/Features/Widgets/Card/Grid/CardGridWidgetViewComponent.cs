using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Card.Grid;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CardGridWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(CardGridWidgetViewComponent),
    name: "Card Grid",
    propertiesType: typeof(CardGridWidgetProperties),
    IconClass = "icon-l-grid-2-2",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Card.Grid;

public class CardGridWidgetViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = "KCC.Web.CardGridWidget";

    public async Task<IViewComponentResult> InvokeAsync(CardGridWidgetProperties properties)
    {
        var cardGuids = properties.Cards?.Select(card => card.Identifier);

        if (cardGuids?.Any() is not true)
        {
            return View("~/Features/Widgets/Card/Grid/CardGridWidget.cshtml", new CardGridWidgetViewModel());
        }

        var cards = (await contentRetriever.RetrieveContent<CardItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), cardGuids)),
            new($"{nameof(CardGridWidgetViewComponent)}|{nameof(InvokeAsync)}|{string.Join(',', cardGuids)}")
        )).OrderBy(item => cardGuids.ToList().IndexOf(item.SystemFields.ContentItemGUID));

        var viewModel = new CardGridWidgetViewModel
        {
            Cards = cards,
            Properties = properties,
        };

        return View("~/Features/Widgets/Card/Grid/CardGridWidget.cshtml", viewModel);
    }
}