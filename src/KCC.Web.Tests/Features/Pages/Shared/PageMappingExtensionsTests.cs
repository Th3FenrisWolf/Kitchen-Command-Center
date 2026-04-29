using KCC;
using KCC.Web.Features.Pages.Home;
using KCC.Web.Features.Pages.Shared;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.Shared;

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

    [Fact]
    public void MapMetadata_CopiesScalarFieldsAndRenamedMetadata()
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

        Assert.Equal("Title", dest.Title);
        Assert.Equal("Desc", dest.Description);
        Assert.Equal("a,b,c", dest.Keywords);
        Assert.Equal(source.MetadataPublishDate.ToString(), dest.PublishDate);
        Assert.True(dest.ShowBreadcrumbs);
        Assert.Equal("summary_large_image", dest.TwitterCard);
        Assert.Equal("@kcc", dest.TwitterSite);
        Assert.Equal("@author", dest.TwitterCreator);
    }

    [Fact]
    public void MapMetadata_NullImagesLeaveImageFieldsAtDefaults()
    {
        var source = new MetadataStub { MetadataImage = null, TwitterImage = null };
        var dest = new HomeViewModel();

        source.MapMetadata(dest);

        Assert.Null(dest.ImagePath);
        Assert.Equal(0, dest.ImageWidth);
        Assert.Equal(0, dest.ImageHeight);
        Assert.Null(dest.TwitterImagePath);
    }

    [Fact]
    public void MapMetadata_EmptyImagesLeaveImageFieldsAtDefaults()
    {
        var source = new MetadataStub
        {
            MetadataImage = Array.Empty<ImageItem>(),
            TwitterImage = Array.Empty<ImageItem>(),
        };
        var dest = new HomeViewModel();

        source.MapMetadata(dest);

        Assert.Null(dest.ImagePath);
        Assert.Equal(0, dest.ImageWidth);
        Assert.Equal(0, dest.ImageHeight);
        Assert.Null(dest.TwitterImagePath);
    }
}
