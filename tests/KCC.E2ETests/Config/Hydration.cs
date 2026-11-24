using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class Hydration
{
    public const string Usable = "() => performance.getEntriesByName('kcc:hydrated').length > 0";

    // A wait polled on animation frames waits for a frame, and the first frame of a page full of washes can take
    // longer than the timeout under CI's software rendering.
    public static Task WaitAsync(IPage page) =>
        page.WaitForFunctionAsync(Usable, null, new() { PollingInterval = 100 });
}
