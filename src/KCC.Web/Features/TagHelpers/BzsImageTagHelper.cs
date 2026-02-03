using CMS.ContentEngine;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KCC.Web.Features.TagHelpers;

public class BzsImageTagHelper(IContentRetriever contentRetriever) : TagHelper
{
    public ContentItemReference Item { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "img";

        if (Item == null)
        {
            return;
        }

        var items = await contentRetriever.RetrieveContent<ImageItem>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .TopN(1)
                .Where(where =>
                    where.WhereEquals(
                        nameof(ImageItem.SystemFields.ContentItemGUID),
                        Item.Identifier
                    )
                ),
            new(
                $"{nameof(BzsImageTagHelper)}|{nameof(ProcessAsync)}|{Item.Identifier}"
            )
        );

        var itemData = items.FirstOrDefault();

        if (itemData == null)
        {
            return;
        }

        output.Attributes.SetAttribute("src", itemData.Asset?.Url);
        output.Attributes.SetAttribute("alt", itemData.AltText);
    }
}
