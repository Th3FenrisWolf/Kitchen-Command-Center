using Microsoft.Extensions.Configuration;

namespace KCC.UnitTests.Features.Configuration;

public class AppSettingsTests
{
    // Umbraco's defaults are a 30-day lockout, 10-character passwords and one session per member, which would sign
    // a member's laptop out within 30 seconds of signing in on their phone.
    [Test]
    [Arguments("appsettings.json", "Umbraco:CMS:Security:MemberDefaultLockoutTimeInMinutes", "15")]
    [Arguments("appsettings.json", "Umbraco:CMS:Security:MemberPassword:MaxFailedAccessAttemptsBeforeLockout", "5")]
    [Arguments("appsettings.json", "Umbraco:CMS:Security:MemberPassword:RequiredLength", "8")]
    [Arguments("appsettings.json", "Umbraco:CMS:Security:MemberAllowConcurrentLogins", "True")]
    [Arguments("appsettings.json", "ConnectionStrings:umbracoDbDSN_ProviderName", "Microsoft.Data.Sqlite")]
    [Arguments("appsettings.json", "Umbraco:CMS:Global:DistributedLockingWriteLockDefaultTimeout", "00:00:30")]
    [Arguments("appsettings.Production.json", "Umbraco:CMS:Runtime:Mode", "Production")]
    // The container's root file system is read-only, and umbraco/Data is the volume that survives a deploy.
    [Arguments("appsettings.Production.json", "Umbraco:CMS:Logging:Directory", "~/umbraco/Data/Logs")]
    // Otherwise a lost database is silently replaced by an empty site; deploy/first-boot.env turns the install on.
    [Arguments("appsettings.Production.json", "Umbraco:CMS:Unattended:InstallUnattended", "False")]
    // Every other environment keeps appsettings.json's deny-all robots.txt: only the live site is meant to be indexed.
    [Arguments("appsettings.Production.json", "RobotsTxtDenyAll", "False")]
    public async Task Setting_HoldsItsValue(string file, string key, string expected)
    {
        _ = await Assert.That(Settings(file)[key]).IsEqualTo(expected);
    }

    [Test]
    public async Task ConnectionString_UsesAPrivateCache()
    {
        var connectionString = Settings("appsettings.json").GetConnectionString("umbracoDbDSN")!;

        _ = await Assert.That(connectionString.Contains("Cache=Shared", StringComparison.OrdinalIgnoreCase)).IsFalse();
        _ = await Assert.That(connectionString.Contains("Cache=Private", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    private static IConfigurationRoot Settings(string file) =>
        new ConfigurationBuilder().AddJsonFile(Path.Combine(RepoPaths.WebProject, file)).Build();
}
