using KCC;
using KCC.Web.Features.Pages.Home;
using KCC.Web.Features.Pages.Shared;

namespace KCC.UnitTests.Features.Pages.Shared;

public class PageMappingExtensionsTests
{
    private sealed class MetadataStub : IMetadata
    {
        public bool ExcludeFromSitemap { get; set; }
        public bool ShowBreadcrumbs { get; set; }
        public string BreadcrumbLabel { get; set; }
        public string MetadataTitle { get; set; }
        public string MetadataDescription { get; set; }
        public IEnumerable<ImageItem> MetadataImage { get; set; }
        public string MetadataKeywords { get; set; }
        public DateTime MetadataPublishDate { get; set; }
        public string TwitterCard { get; set; }
        public string TwitterSite { get; set; }
        public string TwitterCreator { get; set; }
        public IEnumerable<ImageItem> TwitterImage { get; set; }
    }

    [Test]
    public async Task MapMetadata_CopiesScalarFieldsAndRenamedMetadata()
    {
        var source = new MetadataStub
        {
            MetadataTitle = "Title",
            MetadataDescription = "Desc",
            MetadataKeywords = "a,b,c",
            MetadataPublishDate = new DateTime(2026, 4, 23, 10, 30, 0, DateTimeKind.Utc),
            ShowBreadcrumbs = true,
            TwitterCard = "summary_large_image",
            TwitterSite = "@kcc",
            TwitterCreator = "@author",
        };
        var dest = new HomeViewModel();

        source.MapMetadata(dest);

        _ = await Assert.That(dest.Title).IsEqualTo("Title");
        _ = await Assert.That(dest.Description).IsEqualTo("Desc");
        _ = await Assert.That(dest.Keywords).IsEqualTo("a,b,c");
        _ = await Assert.That(dest.PublishDate).IsEqualTo(source.MetadataPublishDate.ToString());
        _ = await Assert.That(dest.ShowBreadcrumbs).IsTrue();
        _ = await Assert.That(dest.TwitterCard).IsEqualTo("summary_large_image");
        _ = await Assert.That(dest.TwitterSite).IsEqualTo("@kcc");
        _ = await Assert.That(dest.TwitterCreator).IsEqualTo("@author");
    }

    [Test]
    public async Task MapMetadata_NullImagesLeaveImageFieldsAtDefaults()
    {
        var source = new MetadataStub { MetadataImage = null, TwitterImage = null };
        var dest = new HomeViewModel();

        source.MapMetadata(dest);

        _ = await Assert.That(dest.ImagePath).IsNull();
        _ = await Assert.That(dest.ImageWidth).IsEqualTo(0);
        _ = await Assert.That(dest.ImageHeight).IsEqualTo(0);
        _ = await Assert.That(dest.TwitterImagePath).IsNull();
    }

    [Test]
    public async Task MapMetadata_EmptyImagesLeaveImageFieldsAtDefaults()
    {
        var source = new MetadataStub
        {
            MetadataImage = Array.Empty<ImageItem>(),
            TwitterImage = Array.Empty<ImageItem>(),
        };
        var dest = new HomeViewModel();

        source.MapMetadata(dest);

        _ = await Assert.That(dest.ImagePath).IsNull();
        _ = await Assert.That(dest.ImageWidth).IsEqualTo(0);
        _ = await Assert.That(dest.ImageHeight).IsEqualTo(0);
        _ = await Assert.That(dest.TwitterImagePath).IsNull();
    }
}
