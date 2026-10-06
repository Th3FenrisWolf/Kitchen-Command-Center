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

    [Test]
    public async Task NavGroup_IsImportedOnFirstBoot()
    {
        using var scope = Site.Services.CreateScope();
        var nav = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetGroup("Nav");

        _ = await Assert.That(nav.Count).IsEqualTo(29);
        _ = await Assert.That(nav["Nav.MyKitchen"]).IsEqualTo("My kitchen");
        _ = await Assert.That(nav["Nav.NothingMatches"]).IsEqualTo("Nothing matches “{0}” yet.");
        _ = await Assert.That(nav["Nav.KitchenOf"]).IsEqualTo("{0}’s kitchen");
    }
}
