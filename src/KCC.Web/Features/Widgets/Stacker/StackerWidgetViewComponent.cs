using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Stacker;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: StackerWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(StackerWidgetViewComponent),
    name: "Stacker",
    propertiesType: typeof(StackerWidgetProperties),
    IconClass = "icon-l-text-col",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Stacker;

public class StackerWidgetViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = "KCC.Web.StackerWidget";

    public async Task<IViewComponentResult> InvokeAsync(StackerWidgetProperties properties)
    {
        var viewModel = new StackerWidgetViewModel
        {
            Heading = properties.Heading,
            Body = properties.Body,
            Image = await RetrieveImage(properties.Image),
            Cards = await RetrieveCards(properties.Cards),
            Properties = properties,
        };

        return View("~/Features/Widgets/Stacker/StackerWidget.cshtml", viewModel);
    }

    private async Task<ImageItem> RetrieveImage(IEnumerable<ContentItemReference> imageReferences)
    {
        if (imageReferences?.Any() is not true)
        {
            return null;
        }

        var imageGuid = imageReferences
            .Select(image => image.Identifier)
            .FirstOrDefault();

        var image = (await contentRetriever.RetrieveContent<ImageItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), imageGuid)),
            new($"{nameof(StackerWidgetViewComponent)}|{nameof(InvokeAsync)}|{imageGuid}")
        )).FirstOrDefault();

        return image;
    }

    private async Task<IEnumerable<CardItem>> RetrieveCards(IEnumerable<ContentItemReference> cardReferences)
    {
        if (cardReferences?.Any() is not true)
        {
            return [];
        }

        var cardGuids = cardReferences.Select(card => card.Identifier);

        var cards = (await contentRetriever.RetrieveContent<CardItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), cardGuids)),
            new($"{nameof(StackerWidgetViewComponent)}|{nameof(InvokeAsync)}|{string.Join(',', cardGuids)}")
        )).OrderBy(item => cardGuids.ToList().IndexOf(item.SystemFields.ContentItemGUID));

        return cards;
    }
}