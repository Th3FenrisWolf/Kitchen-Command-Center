using KCC.E2ETests.Config;
using Microsoft.Playwright;
using TUnit.Playwright;

namespace KCC.E2ETests;

public class BasePageTests : PageTest
{
    public BasePageTests()
        : base(new BrowserTypeLaunchOptions { Headless = true }) { }

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
