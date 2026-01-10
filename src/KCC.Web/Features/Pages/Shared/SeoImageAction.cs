using System.Linq;
using AutoMapper;

namespace KCC.Web.Features.Pages.Shared;

public class SeoImageAction : IMappingAction<INavigation_Metadata, BasePageViewModel>
{
    public void Process(
        INavigation_Metadata source,
        BasePageViewModel destination,
        ResolutionContext context
    )
    {
        var defaultImageInfo = source.MetadataImage.FirstOrDefault()?.Asset;
        var twitterImageInfo = source.TwitterImage.FirstOrDefault()?.Asset;

        destination.ImagePath = defaultImageInfo?.Url;
        destination.ImageWidth = defaultImageInfo?.Metadata.Width ?? 0;
        destination.ImageHeight = defaultImageInfo?.Metadata.Height ?? 0;

        destination.TwitterImagePath = twitterImageInfo?.Url;
    }
}
