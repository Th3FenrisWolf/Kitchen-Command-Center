using KCC.E2ETests.Config;
using Microsoft.Playwright;
using TUnit.Playwright;

namespace KCC.E2ETests;

public class BasePageTests : PageTest
{
    protected Uri RootUri { get; private set; } = new(Constants.TESTING_DOMAIN);

    public BasePageTests()
        : base(new BrowserTypeLaunchOptions { Headless = true }) { }

    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext) ?? new();
        options.ColorScheme = ColorScheme.Light;
        options.ViewportSize = new() { Height = 1080, Width = 1920 };
        options.BaseURL = RootUri.ToString();

        return options;
    }
}
