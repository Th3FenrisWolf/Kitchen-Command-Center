using CMS.ContentEngine;
using KCC.Web.Features.Widgets.Hero;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: SmallHeroWidgetViewComponent.IDENTIFIER,
    viewComponentType: typeof(SmallHeroWidgetViewComponent),
    name: "Small Hero",
    propertiesType: typeof(SmallHeroWidgetProperties),
    IconClass = "icon-rectangle-a-o",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Hero;

public class SmallHeroWidgetViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public const string IDENTIFIER = SmallHeroWidgetProperties.IDENTIFIER;

    public async Task<IViewComponentResult> InvokeAsync(SmallHeroWidgetProperties properties)
    {
        var viewModel = new SmallHeroWidgetViewModel
        {
            Eyebrow = properties.Eyebrow,
            Title = properties.Title,
            Description = properties.Description,
            Dark = properties.Dark,
            ActionLink = (await RetrieveLink(properties.ActionButton))?.MapToPageLink(),
            Properties = properties,
        };

        return View("~/Features/Widgets/Hero/SmallHeroWidget.cshtml", viewModel);
    }

    private async Task<LinkItem> RetrieveLink(IEnumerable<ContentItemReference> linkReferences)
    {
        var linkGuid = linkReferences?.FirstOrDefault()?.Identifier;

        if (linkGuid is null)
        {
            return null;
        }

        return (await contentRetriever.RetrieveContent<LinkItem>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
            query => query.Where(where => where
                .WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), linkGuid)),
            new($"{nameof(SmallHeroWidgetViewComponent)}|{nameof(InvokeAsync)}|{linkGuid}")
        )).FirstOrDefault();
    }
}
