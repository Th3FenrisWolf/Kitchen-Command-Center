using CMS.Websites;

namespace KCC.Web.Features.Pages.Shared;

public static class PageMappingExtensions
{
    public static void MapMetadata<TSource, TViewModel>(this TSource source, TViewModel dest)
        where TSource : IMetadata
        where TViewModel : BasePageViewModel
    {
        dest.Title = source.MetadataTitle;
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
    }

    public static void MapWebPageFields<TViewModel>(this IWebPageFieldsSource source, TViewModel dest)
        where TViewModel : BasePageViewModel
    {
        dest.WebPageItemID = source.SystemFields.WebPageItemID;
    }
}
