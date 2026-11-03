using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Baseline;

public class VariantDetailStringsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("VariantDetail.NoReviewsYet", "No reviews yet.")]
    [Arguments("VariantDetail.NoReviewsHint", "Cook it, then come back and say how it went.")]
    public async Task EmptyReviewsStrings_ImportFromTheBaseline(string key, string expected)
    {
        using var scope = Site.Services.CreateScope();

        _ = await Assert.That(scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key)).IsEqualTo(expected);
    }
}
