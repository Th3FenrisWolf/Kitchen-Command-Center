namespace KCC.UnitTests.Features.Configuration;

// Umbraco writes a generated value into appsettings.json when either key is missing: an HMAC secret on install,
// a site id five minutes into any run.
public class ConfigFileWritesTests
{
    [Test]
    public async Task ImagingHmacSecretKey_IsNeverCommitted()
    {
        var cms = WebAppSettings.Load().GetProperty("Umbraco").GetProperty("CMS");
        var hasKey = cms.TryGetProperty("Imaging", out var imaging) && imaging.TryGetProperty("HMACSecretKey", out _);

        _ = await Assert.That(hasKey).IsFalse();
    }

    [Test]
    public async Task GlobalId_IsANonEmptyGuid()
    {
        var global = WebAppSettings.Load().GetProperty("Umbraco").GetProperty("CMS").GetProperty("Global");
        var id = global.TryGetProperty("Id", out var value) ? value.GetString() : null;

        _ = await Assert.That(Guid.TryParse(id, out var guid)).IsTrue();
        _ = await Assert.That(guid).IsNotEqualTo(Guid.Empty);
    }
}
