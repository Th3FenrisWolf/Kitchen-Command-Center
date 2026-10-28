using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public enum GpuMode
{
    Hardware,
    SwiftShader,
}

public sealed record NetworkConditions(double DownloadBytesPerSecond, double UploadBytesPerSecond, double LatencyMs);

public sealed record Profile(
    string Name,
    int Width,
    int Height,
    float DeviceScaleFactor,
    bool IsMobile,
    NetworkConditions? Network,
    double CpuSlowdown,
    GpuMode? FixedGpu)
{
    public GpuMode Gpu => FixedGpu ?? Profiles.DefaultDesktopGpu();
}

public static class Profiles
{
    public const string GpuVariable = "KCC_PERF_GPU";

    public static readonly Profile Phone = new(
        "phone", 390, 844, 3, IsMobile: true, new NetworkConditions(9_000_000 / 8, 1_500_000 / 8, 170), CpuSlowdown: 4, GpuMode.SwiftShader);

    public static readonly Profile Desktop = new(
        "desktop", 1440, 900, 1, IsMobile: false, Network: null, CpuSlowdown: 1, FixedGpu: null);

    public static Profile ByName(string name) => name switch
    {
        "phone" => Phone,
        "desktop" => Desktop,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown profile."),
    };

    public static GpuMode DefaultDesktopGpu() =>
        Environment.GetEnvironmentVariable(GpuVariable)?.ToLowerInvariant() switch
        {
            "swiftshader" => GpuMode.SwiftShader,
            "hardware" => GpuMode.Hardware,
            _ => Environment.GetEnvironmentVariable("CI") is { Length: > 0 } ? GpuMode.SwiftShader : GpuMode.Hardware,
        };

    // Playwright gives WebKit no GPU switches, so only Chromium gets arguments.
    public static string[] ChromiumArguments(GpuMode gpu) => gpu switch
    {
        GpuMode.SwiftShader => ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"],
        _ when OperatingSystem.IsMacOS() => ["--use-angle=metal", "--enable-gpu", "--ignore-gpu-blocklist", "--enable-gpu-rasterization"],
        _ => ["--enable-gpu", "--ignore-gpu-blocklist", "--enable-gpu-rasterization"],
    };

    public static async Task<IBrowser> LaunchAsync(IPlaywright playwright, Engine engine, GpuMode gpu) =>
        engine == Engine.Chromium
            ? await playwright.Chromium.LaunchAsync(new() { Channel = "chromium", Headless = true, Args = ChromiumArguments(gpu) })
            : await playwright.Webkit.LaunchAsync(new() { Headless = true });

    public static BrowserNewContextOptions ContextOptions(Profile profile, Engine engine, Ramp ramp, Uri baseUrl) => new()
    {
        BaseURL = baseUrl.ToString(),
        IgnoreHTTPSErrors = baseUrl.IsLoopback,
        ViewportSize = new() { Width = profile.Width, Height = profile.Height },
        DeviceScaleFactor = profile.DeviceScaleFactor,
        HasTouch = profile.IsMobile,
        IsMobile = profile.IsMobile && engine == Engine.Chromium,
        ColorScheme = ramp == Ramp.Dark ? ColorScheme.Dark : ColorScheme.Light,
    };

    public static async Task ThrottleAsync(ICDPSession devTools, Profile profile)
    {
        if (profile.Network is { } network)
        {
            await devTools.SendAsync("Network.enable");
            await devTools.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
            {
                ["offline"] = false,
                ["latency"] = network.LatencyMs,
                ["downloadThroughput"] = network.DownloadBytesPerSecond,
                ["uploadThroughput"] = network.UploadBytesPerSecond,
            });
        }

        await SetCpuAsync(devTools, profile.CpuSlowdown);
    }

    public static Task SetCpuAsync(ICDPSession devTools, double rate) =>
        devTools.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = rate });
}
