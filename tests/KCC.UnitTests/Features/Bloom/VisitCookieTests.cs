using KCC.Web.Features.Bloom;

namespace KCC.UnitTests.Features.Bloom;

public class VisitCookieTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_792_000_300);

    [Test]
    public async Task Format_WritesVersionHexSeedAndUnixStart()
    {
        var visit = new Visit(0x9f3a0c71, DateTimeOffset.FromUnixTimeSeconds(1_792_000_000), Now);

        _ = await Assert.That(VisitCookie.Format(visit)).IsEqualTo("1.9f3a0c71.1792000000");
    }

    [Test]
    public async Task Format_PadsASmallSeedToEightDigits()
    {
        var visit = new Visit(0xab, DateTimeOffset.FromUnixTimeSeconds(1_792_000_000), Now);

        _ = await Assert.That(VisitCookie.Format(visit)).IsEqualTo("1.000000ab.1792000000");
    }

    [Test]
    [Arguments(0u)]
    [Arguments(0x9f3a0c71u)]
    [Arguments(uint.MaxValue)]
    public async Task Parse_RoundTripsFormat(uint seed)
    {
        var visit = new Visit(seed, DateTimeOffset.FromUnixTimeSeconds(1_792_000_000), Now);

        var parsed = VisitCookie.Parse(VisitCookie.Format(visit), Now);

        _ = await Assert.That(parsed).IsEqualTo(visit);
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    [Arguments("1.9f3a0c71")]
    [Arguments("1.9f3a0c71.1792000000.0")]
    [Arguments("2.9f3a0c71.1792000000")]
    [Arguments("01.9f3a0c71.1792000000")]
    [Arguments("1.9f3a0c7g.1792000000")]
    [Arguments("1.9F3A0C71.1792000000")]
    [Arguments("1.9f3a0c7.1792000000")]
    [Arguments("1.9f3a0c711.1792000000")]
    [Arguments("1.-f3a0c71.1792000000")]
    [Arguments("1.9f3a0c71.abc")]
    [Arguments("1.9f3a0c71.-1")]
    [Arguments("1.9f3a0c71.+1792000000")]
    [Arguments("1.9f3a0c71.99999999999999999999999")]
    public async Task Parse_MalformedValue_ReturnsNull(string value)
    {
        _ = await Assert.That(VisitCookie.Parse(value, Now)).IsNull();
    }

    [Test]
    public async Task Parse_StartInTheFuture_ReturnsNull()
    {
        var value = $"1.9f3a0c71.{Now.ToUnixTimeSeconds() + 1}";

        _ = await Assert.That(VisitCookie.Parse(value, Now)).IsNull();
    }

    [Test]
    public async Task Parse_StartMoreThanAYearBack_ReturnsNull()
    {
        var value = $"1.9f3a0c71.{Now.AddDays(-365).ToUnixTimeSeconds() - 1}";

        _ = await Assert.That(VisitCookie.Parse(value, Now)).IsNull();
    }

    [Test]
    public async Task Parse_StartExactlyAYearBack_IsKept()
    {
        var value = $"1.9f3a0c71.{Now.AddDays(-365).ToUnixTimeSeconds()}";

        _ = await Assert.That(VisitCookie.Parse(value, Now)).IsNotNull();
    }

    [Test]
    public async Task Parse_StartNow_IsKept()
    {
        var value = $"1.9f3a0c71.{Now.ToUnixTimeSeconds()}";

        _ = await Assert.That(VisitCookie.Parse(value, Now)?.StartedAt).IsEqualTo(Now);
    }
}
