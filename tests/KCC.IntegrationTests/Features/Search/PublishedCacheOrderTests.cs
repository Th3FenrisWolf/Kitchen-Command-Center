using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Spec §19: the content cache refresher's notification fires after the published cache is current, so a rebuild it
// triggers reads the change it was triggered by.
public class PublishedCacheOrderTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private PublishedCacheProbe Probe => Site.Services.GetRequiredService<PublishedCacheProbe>();

    [Test]
    public async Task Publishing_IsInTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Fresh");

        var sighting = Probe.LastSighting(key);

        _ = await Assert.That(sighting?.PublishedName).IsEqualTo("IT Refresher Fresh");
        _ = await Assert.That(sighting?.PublishedUrl).IsEqualTo("/recipes/it-refresher-fresh/");
    }

    [Test]
    public async Task ARename_IsInTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Before");

        await TestContent.RenameAsync(Site.Services, key, "IT Refresher After");

        var sighting = Probe.LastSighting(key);
        _ = await Assert.That(sighting?.PublishedName).IsEqualTo("IT Refresher After");
        _ = await Assert.That(sighting?.PublishedUrl).IsEqualTo("/recipes/it-refresher-after/");
    }

    [Test]
    public async Task AnUnpublish_IsOutOfTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Withdrawn");

        await TestContent.UnpublishAsync(Site.Services, key);

        var sighting = Probe.LastSighting(key);
        _ = await Assert.That(sighting).IsNotNull();
        _ = await Assert.That(sighting!.PublishedName).IsNull();
    }
}
