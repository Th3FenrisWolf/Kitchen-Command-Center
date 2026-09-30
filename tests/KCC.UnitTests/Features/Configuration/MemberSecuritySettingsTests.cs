namespace KCC.UnitTests.Features.Configuration;

// Umbraco's own defaults are a 30-day lockout, 10-character passwords and one session per member.
public class MemberSecuritySettingsTests
{
    [Test]
    public async Task Lockout_IsFiveFailuresForFifteenMinutes()
    {
        var security = Security();

        _ = await Assert.That(security.GetProperty("MemberDefaultLockoutTimeInMinutes").GetInt32()).IsEqualTo(15);
        _ = await Assert.That(security.GetProperty("MemberPassword").GetProperty("MaxFailedAccessAttemptsBeforeLockout").GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task Passwords_NeedEightCharacters()
    {
        _ = await Assert.That(Security().GetProperty("MemberPassword").GetProperty("RequiredLength").GetInt32()).IsEqualTo(8);
    }

    // With one session per member, signing in on a phone would sign the laptop out within 30 seconds.
    [Test]
    public async Task Members_CanStaySignedInOnSeveralDevices()
    {
        _ = await Assert.That(Security().GetProperty("MemberAllowConcurrentLogins").GetBoolean()).IsTrue();
    }

    private static System.Text.Json.JsonElement Security() =>
        WebAppSettings.Load().GetProperty("Umbraco").GetProperty("CMS").GetProperty("Security");
}
