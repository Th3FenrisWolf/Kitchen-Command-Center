using KCC.E2ETests.Config;
using Microsoft.Playwright;
using TUnit.Playwright;

namespace KCC.E2ETests;

public class BasePageTests : PageTest
{
    // Browsers are pooled per worker, and a test class's own launch options reach only a worker with no browser yet.
    public BasePageTests()
        : base(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream"],
        })
    {
    }

    [ClassDataSource<SiteProcess>(Shared = SharedType.PerTestSession)]
    public SiteProcess Site { get; init; } = null!;

    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext) ?? new();
        options.ColorScheme = ColorScheme.Light;
        options.ViewportSize = new() { Height = 1080, Width = 1920 };
        options.BaseURL = Site.BaseUrl.ToString();
        options.IgnoreHTTPSErrors = true;

        return options;
    }
}
