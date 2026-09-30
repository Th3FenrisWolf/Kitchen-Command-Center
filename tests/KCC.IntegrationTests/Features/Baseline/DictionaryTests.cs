using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Baseline;

public class DictionaryTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ExportedString_IsImportedOnFirstBoot()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Login.SignIn")).IsNotEqualTo("Login.SignIn");
        _ = await Assert.That(strings.GetOrDefault("Shared.LogoAlt")).IsEqualTo("Kitchen Command Center");
    }

    [Test]
    public async Task UnknownKey_FallsBackToTheKey()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Nowhere.ToBeFound")).IsEqualTo("Nowhere.ToBeFound");
    }
}
