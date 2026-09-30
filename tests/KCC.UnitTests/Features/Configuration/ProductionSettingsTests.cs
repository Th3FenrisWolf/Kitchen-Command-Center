using System.Text.Json;

namespace KCC.UnitTests.Features.Configuration;

public class ProductionSettingsTests
{
    [Test]
    public async Task Production_RunsInUmbracosProductionMode()
    {
        var mode = Cms().GetProperty("Runtime").GetProperty("Mode").GetString();

        _ = await Assert.That(mode).IsEqualTo("Production");
    }

    // The container's root file system is read-only, and umbraco/Data is the volume that survives a deploy.
    [Test]
    public async Task Production_LogsToTheDataVolume()
    {
        var directory = Cms().GetProperty("Logging").GetProperty("Directory").GetString();

        _ = await Assert.That(directory).IsEqualTo("~/umbraco/Data/Logs");
    }

    // Otherwise a lost database is silently replaced by an empty site. The first boot turns the install on for
    // itself, in deploy/first-boot.env.
    [Test]
    public async Task Production_NeverInstallsASiteByItself()
    {
        var install = Cms().GetProperty("Unattended").GetProperty("InstallUnattended").GetBoolean();

        _ = await Assert.That(install).IsFalse();
    }

    private static JsonElement Cms() =>
        WebAppSettings.Load("appsettings.Production.json").GetProperty("Umbraco").GetProperty("CMS");
}
