using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Pages.Home;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Pages.Home;

public class HomeImagesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task StackerImage_IsAWebpBrowsersKeepForAYear()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Drinks photo");

        string url;
        using (var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext())
        {
            var media = context.UmbracoContext.Media!.GetById(imageKey)!;
            url = HomeImages.StackerUrl(new MediaWithCrops(media, Site.Services.GetRequiredService<IPublishedValueFallback>(), new ImageCropperValue()));
        }

        using var client = Site.CreateClient();
        using var image = await client.GetAsync(url);

        _ = await Assert.That(url.Contains($"width={HomeImages.StackerWidth}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(url.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(image.Content.Headers.ContentType!.MediaType).IsEqualTo("image/webp");
        _ = await Assert.That(image.Headers.CacheControl!.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
    }
}
