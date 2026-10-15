using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Security;

namespace KCC.UnitTests.Features.RecipeImport;

public class RecipeImportOptionsTests
{
    [Test]
    public async Task ASiteThatConfiguresNothing_GetsTheContractsDefaults()
    {
        var options = new RecipeImportOptions();

        _ = await Assert.That(options.Model).IsEqualTo("claude-opus-5-5");
        _ = await Assert.That(options.Effort).IsEqualTo("low");
        _ = await Assert.That(options.MonthlyCeilingUsd).IsEqualTo(2.00m);
        _ = await Assert.That(options.MaxImageEdge).IsEqualTo(2000);
        _ = await Assert.That(options.ReadTimeoutSeconds).IsEqualTo(85);
        _ = await Assert.That(options.StandInReader).IsFalse();
        _ = await Assert.That(options.StandInDelaySeconds).IsEqualTo(0);
    }

    [Test]
    public async Task ASiteThatConfiguresNothing_AllowsTenReadsPerClientPerHour()
    {
        _ = await Assert.That(new RateLimitOptions().ImportsPerHour).IsEqualTo(10);
    }
}
