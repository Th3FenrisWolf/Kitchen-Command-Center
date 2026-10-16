using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Extensions;

namespace KCC.IntegrationTests.Features.Media;

public class TiffUploadTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private ContentSettings Content => Site.Services.GetRequiredService<IOptions<ContentSettings>>().Value;

    [Test]
    [Arguments("tif")]
    [Arguments("tiff")]
    public async Task TheMediaLibrary_RefusesTiffUploads(string extension)
    {
        _ = await Assert.That(Content.IsFileAllowedForUpload(extension)).IsFalse();
    }

    [Test]
    [Arguments("jpg")]
    [Arguments("png")]
    [Arguments("webp")]
    public async Task TheMediaLibrary_StillTakesTheSiteImageFormats(string extension)
    {
        _ = await Assert.That(Content.IsFileAllowedForUpload(extension)).IsTrue();
    }

    [Test]
    [Arguments("aspx")]
    [Arguments("config")]
    [Arguments("cshtml")]
    public async Task TheMediaLibrary_KeepsUmbracosDefaultRefusals(string extension)
    {
        _ = await Assert.That(Content.IsFileAllowedForUpload(extension)).IsFalse();
    }
}
