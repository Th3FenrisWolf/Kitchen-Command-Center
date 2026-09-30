namespace KCC.UnitTests.Features.Configuration;

public class SqliteConnectionStringTests
{
    [Test]
    public async Task ConnectionString_NeverUsesSharedCache()
    {
        var connectionString = WebAppSettings.Load().GetProperty("ConnectionStrings")
            .GetProperty("umbracoDbDSN").GetString();

        _ = await Assert.That(connectionString.Contains("Cache=Shared", StringComparison.OrdinalIgnoreCase)).IsFalse();
        _ = await Assert.That(connectionString.Contains("Cache=Private", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task ConnectionString_UsesTheSqliteProvider()
    {
        var provider = WebAppSettings.Load().GetProperty("ConnectionStrings")
            .GetProperty("umbracoDbDSN_ProviderName").GetString();

        _ = await Assert.That(provider).IsEqualTo("Microsoft.Data.Sqlite");
    }

    [Test]
    public async Task WriteLockWait_IsThirtySeconds()
    {
        var timeout = WebAppSettings.Load().GetProperty("Umbraco").GetProperty("CMS").GetProperty("Global")
            .GetProperty("DistributedLockingWriteLockDefaultTimeout").GetString();

        _ = await Assert.That(timeout).IsEqualTo("00:00:30");
    }
}
