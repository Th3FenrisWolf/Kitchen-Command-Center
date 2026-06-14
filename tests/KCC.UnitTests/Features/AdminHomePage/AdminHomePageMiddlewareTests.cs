using CMS.Core;
using KCC.Web.Features.AdminHomePage;
using Microsoft.Extensions.DependencyInjection;
using Vite.AspNetCore;

namespace KCC.UnitTests.Features.AdminHomePage;

[NotInParallel]
public class AdminHomePageMiddlewareTests
{
    private const string SourceKey = "Features/AdminHomePage/admin-home-redirect.ts";

    private static readonly StubDevServerStatus DevServer = new();
    private static readonly StubManifest Manifest = new();

    static AdminHomePageMiddlewareTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IViteDevServerStatus>(DevServer);
        services.AddSingleton<IViteManifest>(Manifest);
        Service.SetProvider(services.BuildServiceProvider());
    }

    [Test]
    public async Task BuildScriptTag_DevServerEnabled_UsesSourcePath()
    {
        ConfigureStubs(isEnabled: true, basePath: "/");

        var tag = AdminHomePageMiddleware.BuildScriptTag();

        _ = await Assert.That(tag).IsEqualTo(
            $"<script type=\"module\" src=\"/{SourceKey}\"></script>");
    }

    [Test]
    public async Task BuildScriptTag_DevServerEnabledWithBasePath_PrependsBasePath()
    {
        ConfigureStubs(isEnabled: true, basePath: "/app/");

        var tag = AdminHomePageMiddleware.BuildScriptTag();

        _ = await Assert.That(tag).IsEqualTo(
            $"<script type=\"module\" src=\"/app/{SourceKey}\"></script>");
    }

    [Test]
    public async Task BuildScriptTag_DevServerDisabled_UsesManifestHashedFile()
    {
        ConfigureStubs(isEnabled: false, basePath: "/");
        Manifest[SourceKey] = new StubChunk { File = "assets/adminHomeRedirect-ABC123.js" };

        var tag = AdminHomePageMiddleware.BuildScriptTag();

        _ = await Assert.That(tag).IsEqualTo(
            "<script type=\"module\" src=\"/assets/adminHomeRedirect-ABC123.js\"></script>");
    }

    [Test]
    public async Task BuildScriptTag_DevServerDisabledAndManifestMissingEntry_Throws()
    {
        ConfigureStubs(isEnabled: false, basePath: "/");

        var ex = await Assert.That(() => AdminHomePageMiddleware.BuildScriptTag()).Throws<InvalidOperationException>();

        _ = await Assert.That(ex!.Message).Contains(SourceKey);
    }

    private static void ConfigureStubs(bool isEnabled, string basePath)
    {
        DevServer.IsEnabled = isEnabled;
        DevServer.BasePath = basePath;
        Manifest.Clear();
    }

    private sealed class StubDevServerStatus : IViteDevServerStatus
    {
        public bool IsEnabled { get; set; }
        public bool IsMiddlewareEnable => IsEnabled;
        public string ServerUrl => "http://localhost:5173";
        public string BasePath { get; set; } = string.Empty;
        public string ServerUrlWithBasePath => $"{ServerUrl}{BasePath}";
    }

    private sealed class StubManifest : IViteManifest
    {
        private readonly Dictionary<string, IViteChunk> _entries = new(StringComparer.Ordinal);

        public IViteChunk this[string key]
        {
            get => _entries.TryGetValue(key, out var chunk) ? chunk : null;
            set
            {
                if (value is null) _entries.Remove(key);
                else _entries[key] = value;
            }
        }

        public bool ContainsKey(string key) => _entries.ContainsKey(key);

        public IEnumerable<string> Keys => _entries.Keys;

        public void Clear() => _entries.Clear();

        public IEnumerator<IViteChunk> GetEnumerator() => _entries.Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StubChunk : IViteChunk
    {
        public string File { get; set; } = string.Empty;
        public string Src { get; set; } = string.Empty;
        public bool? IsEntry { get; set; }
        public bool? IsDynamicEntry { get; set; }
        public IEnumerable<string> Css { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Assets { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Imports { get; set; } = Array.Empty<string>();
        public IEnumerable<string> DynamicImports { get; set; } = Array.Empty<string>();
    }
}
