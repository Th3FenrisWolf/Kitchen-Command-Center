using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Card;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CardWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(CardWidgetViewComponent),
    name: "Card",
    propertiesType: typeof(CardWidgetProperties),
    IconClass = "icon-square",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Card;

public class CardWidgetViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = "KCC.Web.CardWidget";

    public async Task<IViewComponentResult> InvokeAsync(CardWidgetProperties properties)
    {
        var cardGuid = properties.Card?.FirstOrDefault()?.Identifier;

        if (cardGuid is null)
        {
            return View("~/Features/Widgets/Card/CardWidget.cshtml", new CardWidgetViewModel());
        }

        var card = (await contentRetriever.RetrieveContent<CardItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), cardGuid)),
            new($"{nameof(CardWidgetViewComponent)}|{nameof(InvokeAsync)}|{cardGuid}")
        )).FirstOrDefault();

        var viewModel = new CardWidgetViewModel
        {
            Card = card,
            Properties = properties,
        };

        return View("~/Features/Widgets/Card/CardWidget.cshtml", viewModel);
    }
}