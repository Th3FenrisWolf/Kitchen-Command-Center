using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using KitchenCommandCenter.Web.Features.Widgets.Card;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CardWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(CardWidgetViewComponent),
    name: "Card",
    propertiesType: typeof(CardWidgetProperties),
    IconClass = "icon-card",
    AllowCache = true
)]

namespace KitchenCommandCenter.Web.Features.Widgets.Card;

public class CardWidgetViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = "KitchenCommandCenter.Web.CardWidget";

    public async Task<IViewComponentResult> InvokeAsync(CardWidgetProperties properties)
    {
        var cardGuids = properties.Cards?.Select(card => card.Identifier);

        if (cardGuids?.Any() is not true)
        {
            return View("~/Features/Widgets/Card/CardWidget.cshtml", new CardWidgetViewModel());
        }

        var cards = (await contentRetriever.RetrieveContent<CardItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), cardGuids)),
            new($"{nameof(CardWidgetViewComponent)}|{nameof(InvokeAsync)}|{string.Join(',', cardGuids)}")
        )).OrderBy(item => cardGuids.ToList().IndexOf(item.SystemFields.ContentItemGUID));

        var viewModel = new CardWidgetViewModel
        {
            Cards = cards,
            Properties = properties,
        };

        return View("~/Features/Widgets/Card/CardWidget.cshtml", viewModel);
    }
}