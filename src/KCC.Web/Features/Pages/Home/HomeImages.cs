using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Home;

public static class HomeImages
{
    // Half a container-width section, the default (80rem), is about 626px of text column; twice that covers
    // high-density screens. A breakout or full-width section shows the image a little soft.
    public const int StackerWidth = 1280;

    public static string StackerUrl(MediaWithCrops image) => image?.GetCropUrl(width: StackerWidth, furtherOptions: "&format=webp");
}
