using CMS.Websites;
using KCC.Web.Features.Extensions;

namespace KCC.Web.Features.Pages.Shared;

public static class PageMappingExtensions
{
    public static async Task MapMetadata<TSource, TViewModel>(this TSource source, TViewModel dest)
        where TSource : IMetadata, IWebPageFieldsSource
        where TViewModel : BasePageViewModel
    {
        dest.Title = await source.GetMetadataTitle();
        dest.Description = source.MetadataDescription;
        dest.Keywords = source.MetadataKeywords;
        dest.PublishDate = source.MetadataPublishDate.ToString();

        dest.ShowBreadcrumbs = source.ShowBreadcrumbs;
        dest.TwitterCard = source.TwitterCard;
        dest.TwitterSite = source.TwitterSite;
        dest.TwitterCreator = source.TwitterCreator;

        var defaultImage = source.MetadataImage?.FirstOrDefault()?.Asset;
        var twitterImage = source.TwitterImage?.FirstOrDefault()?.Asset;

        dest.ImagePath = defaultImage?.Url;
        dest.ImageWidth = defaultImage?.Metadata.Width ?? 0;
        dest.ImageHeight = defaultImage?.Metadata.Height ?? 0;
        dest.TwitterImagePath = twitterImage?.Url;

        dest.WebPageItemID = source.SystemFields.WebPageItemID;
    }
}
