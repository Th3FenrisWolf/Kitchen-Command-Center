using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Routing;

namespace KCC.UnitTests.Features.Pages.Shared;

public class PageMetadataTests
{
    private static readonly TimeZoneInfo UtcPlusFive =
        TimeZoneInfo.CreateCustomTimeZone("UTC+05", TimeSpan.FromHours(5), "UTC+05", "UTC+05");

    [Test]
    public async Task Apply_WithoutMetadataTitle_FallsBackToTheNodeName()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.Title).IsEqualTo("Home");
    }

    [Test]
    public async Task Apply_WithMetadataTitle_UsesIt()
    {
        var viewModel = Apply(Page(title: "Kitchen Command Center"));

        _ = await Assert.That(viewModel.Title).IsEqualTo("Kitchen Command Center");
    }

    [Test]
    public async Task Apply_PublishDate_IsTheCreateDateInIso8601()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.PublishDate).IsEqualTo("2026-09-23T10:00:00.0000000Z");
    }

    [Test]
    [Arguments(DateTimeKind.Utc, 10)]
    [Arguments(DateTimeKind.Unspecified, 10)]
    [Arguments(DateTimeKind.Local, 15)]
    public async Task AsUtc_ReadsOnlyALocalDateInTheLocalZone(DateTimeKind kind, int hour)
    {
        var utc = PageMetadata.AsUtc(new DateTime(2026, 9, 23, hour, 0, 0, kind), UtcPlusFive);

        _ = await Assert.That(utc).IsEqualTo(new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc));
        _ = await Assert.That(utc.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    [Test]
    public async Task Apply_WithImage_ResolvesItsUrlAndSize()
    {
        var media = new Mock<IPublishedContent>();
        media.Setup(m => m.GetProperty("umbracoWidth")).Returns(Property(1200));
        media.Setup(m => m.GetProperty("umbracoHeight")).Returns(Property(630));
        var image = new MediaWithCrops(media.Object, Mock.Of<IPublishedValueFallback>(), new ImageCropperValue());

        var urls = new Mock<IPublishedUrlProvider>();
        urls.Setup(u => u.GetMediaUrl(media.Object, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>()))
            .Returns("/media/og.webp");

        var page = Page(title: string.Empty);
        page.SetupGet(p => p.MetadataImage).Returns(image);
        var viewModel = new BasePageViewModel();
        new PageMetadata(urls.Object).Apply(page.Object, viewModel);

        _ = await Assert.That(viewModel.ImagePath).IsEqualTo("/media/og.webp");
        _ = await Assert.That(viewModel.ImageWidth).IsEqualTo(1200);
        _ = await Assert.That(viewModel.ImageHeight).IsEqualTo(630);
    }

    [Test]
    public async Task Apply_WithoutImages_LeavesImagePathsEmpty()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.ImagePath).IsNull();
        _ = await Assert.That(viewModel.TwitterImagePath).IsNull();
    }

    private static BasePageViewModel Apply(Mock<IMetadata> page)
    {
        var viewModel = new BasePageViewModel();
        new PageMetadata(Mock.Of<IPublishedUrlProvider>()).Apply(page.Object, viewModel);
        return viewModel;
    }

    private static Mock<IMetadata> Page(string title)
    {
        var page = new Mock<IMetadata>();
        page.SetupGet(p => p.Name).Returns("Home");
        page.SetupGet(p => p.MetadataTitle).Returns(title);
        page.SetupGet(p => p.CreateDate).Returns(new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc));
        return page;
    }

    private static IPublishedProperty Property(int value)
    {
        var property = new Mock<IPublishedProperty>();
        property.Setup(p => p.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(value);
        return property.Object;
    }
}
