using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using KCC.Web.Features.Cache;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KCC.Web.Features.TagHelpers;

public class BzsImageTagHelper(ICacheService cacheService) : TagHelper
{
    public ContentItemReference Item { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "img";

        if (Item == null)
        {
            return;
        }

        var query = new ContentItemQueryBuilder()
            .ForContentTypes(config =>
                config.OfContentType(ImageItem.CONTENT_TYPE_NAME).WithContentTypeFields()
            )
            .Parameters(param =>
                param
                    .TopN(1)
                    .Where(where =>
                        where.WhereEquals(
                            nameof(ImageItem.SystemFields.ContentItemGUID),
                            Item.Identifier
                        )
                    )
            );

        var itemData = (
            await cacheService.Get<ImageItem>(
                query,
                [nameof(BzsImageTagHelper), nameof(ProcessAsync), Item.Identifier.ToString()]
            )
        ).FirstOrDefault();

        if (itemData == null)
        {
            return;
        }

        output.Attributes.SetAttribute("src", itemData.Asset?.Url);
        output.Attributes.SetAttribute("alt", itemData.AltText);
    }
}
