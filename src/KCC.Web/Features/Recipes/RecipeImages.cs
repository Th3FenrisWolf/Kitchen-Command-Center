using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace KCC.Web.Features.Recipes;

public static class RecipeImages
{
    // AccentTile draws a picture at most 96px square (size-24, in the hero and the featured card). Twice that
    // covers high-density screens, and one size for every tile lets a browser reuse the file across pages.
    public const int TileSize = 192;

    public static string TileUrl(MediaWithCrops image) => image?.GetCropUrl(
        width: TileSize,
        height: TileSize,
        imageCropMode: ImageCropMode.Crop,
        furtherOptions: "&format=webp");
}
