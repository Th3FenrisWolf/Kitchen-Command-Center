namespace KCC.IntegrationTests.Features.Hosting;

// Umbraco retries a stranded SQLite write for about ten minutes, so without a limit one lock costs the job ten minutes.
public class TestTimeoutTests
{
    [Test]
    public async Task EveryTest_StopsAfterTwoMinutes()
    {
        _ = await Assert.That(TestContext.Current!.Metadata.TestDetails.Timeout).IsEqualTo(TimeSpan.FromMinutes(2));
    }
}
