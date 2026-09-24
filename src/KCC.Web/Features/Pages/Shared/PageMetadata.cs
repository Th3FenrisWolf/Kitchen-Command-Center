using System.Globalization;
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace KCC.Web.Features.Pages.Shared;

public class PageMetadata(IPublishedUrlProvider urlProvider)
{
    public void Apply(IMetadata page, BasePageViewModel viewModel)
    {
        viewModel.Title = string.IsNullOrWhiteSpace(page.MetadataTitle) ? page.Name : page.MetadataTitle;
        viewModel.Description = page.MetadataDescription;
        viewModel.Keywords = page.MetadataKeywords;
        viewModel.PublishDate = AsUtc(page.CreateDate, TimeZoneInfo.Local).ToString("o", CultureInfo.InvariantCulture);
        viewModel.ShowBreadcrumbs = page.ShowBreadcrumbs;
        viewModel.TwitterCard = page.TwitterCard;
        viewModel.TwitterSite = page.TwitterSite;
        viewModel.TwitterCreator = page.TwitterCreator;

        if (page.MetadataImage is { } image)
        {
            viewModel.ImagePath = urlProvider.GetMediaUrl(image.Content);
            viewModel.ImageWidth = Dimension(image.Content, "umbracoWidth");
            viewModel.ImageHeight = Dimension(image.Content, "umbracoHeight");
        }

        if (page.TwitterImage is { } twitterImage)
        {
            viewModel.TwitterImagePath = urlProvider.GetMediaUrl(twitterImage.Content);
        }
    }

    // Umbraco stores system dates in UTC, so one that arrives without a Kind is UTC rather than server-local.
    // The zone is a parameter so tests can pin that on a UTC machine, where local and UTC read the same.
    internal static DateTime AsUtc(DateTime value, TimeZoneInfo localZone) => value.Kind switch
    {
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        DateTimeKind.Local => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(value, DateTimeKind.Unspecified), localZone),
        _ => value,
    };

    private static int Dimension(IPublishedContent media, string alias) =>
        media.GetProperty(alias)?.GetValue() is int value ? value : 0;
}
