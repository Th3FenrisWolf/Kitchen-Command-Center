using CMS.ContentEngine;
using KCC.Web.Features.Widgets.ImageCarousel;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ImageCarouselViewComponent.IDENTIFIER,
    viewComponentType: typeof(ImageCarouselViewComponent),
    name: "Image Carousel",
    propertiesType: typeof(ImageCarouselProperties),
    IconClass = "icon-carousel",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.ImageCarousel;

public class ImageCarouselViewComponent(
    IPageBuilderComponentPropertiesRetriever componentPropertiesRetriever,
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = "KCC.Web.ImageCarousel";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var properties = componentPropertiesRetriever.Retrieve<ImageCarouselProperties>();

        if (!Enum.TryParse<ScrollBehavior>(properties.ScrollBehavior, out var scrollBehavior))
        {
            scrollBehavior = ScrollBehavior.Manual;
        }

        var viewModel = new ImageCarouselWidgetViewModel
        {
            Items = await GetCarouselItems(properties.CarouselItems),
            CropImages = properties.CropImages,
            DisplayCount = properties.DisplayCount,
            ScrollBehavior = scrollBehavior,
            Properties = properties,
        };

        return View("~/Features/Widgets/ImageCarousel/_ImageCarousel.cshtml", viewModel);
    }

    private async Task<IEnumerable<ImageItem>> GetCarouselItems(
        IEnumerable<ContentItemReference> contentItems
    )
    {
        if (!contentItems?.Any() ?? true)
        {
            return [];
        }

        var itemGuids = contentItems.Select(item => item.Identifier).ToList();

        var items = await contentRetriever.RetrieveContent<ImageItem>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .Where(where => where
                    .WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), itemGuids)
                ),
            new(
                $"{nameof(ImageCarouselViewComponent)}|{nameof(GetCarouselItems)}|{string.Join("|", itemGuids)}"
            )
        );

        return items.OrderBy(item => itemGuids.IndexOf(item.SystemFields.ContentItemGUID));
    }
}
