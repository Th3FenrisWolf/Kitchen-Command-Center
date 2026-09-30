using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Backoffice;

public class BackofficePackageTests
{
    private const string Manifests = "/umbraco/management/api/v1/manifest/manifest/private";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("KCC.Admin", "KCC Admin")]
    [Arguments("KCC.Contributions", "KCC Contributions")]
    public async Task Package_IsFoundByTheBackoffice_AndItsBundleIsServed(string id, string name)
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        using var anonymous = Site.CreateClient();

        var package = (await admin.GetJsonAsync(Manifests)).EnumerateArray()
            .SingleOrDefault(candidate => candidate.GetProperty("id").GetString() == id);
        if (package.ValueKind == JsonValueKind.Undefined)
        {
            // The bundles are build output, so a missing package usually means the clients were never built.
            throw new InvalidOperationException($"The backoffice has no {id} package. Run `yarn build:all` at the repository root first.");
        }

        var bundle = package.GetProperty("extensions").EnumerateArray().Single();
        var script = bundle.GetProperty("js").GetString()!;
        using var served = await anonymous.GetAsync(script);

        _ = await Assert.That(package.GetProperty("name").GetString()).IsEqualTo(name);
        _ = await Assert.That(bundle.GetProperty("type").GetString()).IsEqualTo("bundle");
        _ = await Assert.That(script).StartsWith($"/App_Plugins/{id}/bundle-");
        _ = await Assert.That(served.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
