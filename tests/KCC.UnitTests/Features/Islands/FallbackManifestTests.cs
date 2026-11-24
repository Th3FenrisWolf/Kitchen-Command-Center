using KCC.Web.Features.Islands;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace KCC.UnitTests.Features.Islands;

public class FallbackManifestTests
{
    private const string WithEntry = """{"Features/Fallback.ts":{"file":"assets/fallback-f1.js","isEntry":true}}""";
    private const string WithoutEntry = """{"Features/Other.ts":{"file":"assets/other-o1.js","isEntry":true}}""";

    [Test]
    public async Task AManifestWrittenAfterAMiss_IsPickedUp()
    {
        using var root = new TempWebRoot();
        var manifest = Create(root, out _);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsFalse();
        root.WriteManifest(WithEntry);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsTrue();
    }

    [Test]
    public async Task AnUnreadableManifestWrittenOver_IsPickedUp()
    {
        using var root = new TempWebRoot();
        root.WriteManifest("{ not json");
        var manifest = Create(root, out _);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsFalse();
        root.WriteManifest(WithEntry);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsTrue();
    }

    [Test]
    public async Task AManifestWithTheEntry_IsReadOnce()
    {
        using var root = new TempWebRoot();
        root.WriteManifest(WithEntry);
        var manifest = Create(root, out var logger);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsTrue();
        root.DeleteManifest();

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsTrue();
        _ = await Assert.That(logger.Warnings).IsEmpty();
    }

    [Test]
    public async Task AMissingManifest_LogsOneWarning_NotOnePerRead()
    {
        using var root = new TempWebRoot();
        var manifest = Create(root, out var logger);

        _ = manifest.ContainsKey("Features/Fallback.ts");
        _ = manifest.ContainsKey("Features/Fallback.ts");
        _ = manifest.Keys.ToList();

        _ = await Assert.That(logger.Warnings).Count().IsEqualTo(1);
        _ = await Assert.That(logger.Warnings[0]).Contains("manifest.json");
    }

    [Test]
    public async Task AManifestWithoutTheEntry_LogsAWarning_AndIsNotCached()
    {
        using var root = new TempWebRoot();
        root.WriteManifest(WithoutEntry);
        var manifest = Create(root, out var logger);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsFalse();
        _ = await Assert.That(logger.Warnings).Count().IsEqualTo(1);
        _ = await Assert.That(logger.Warnings[0]).Contains("Features/Fallback.ts");
        root.WriteManifest(WithEntry);

        _ = await Assert.That(manifest.ContainsKey("Features/Fallback.ts")).IsTrue();
    }

    [Test]
    public async Task AnUnreadableManifest_LogsAWarning()
    {
        using var root = new TempWebRoot();
        root.WriteManifest("{ not json");
        var manifest = Create(root, out var logger);

        _ = manifest.ContainsKey("Features/Fallback.ts");

        _ = await Assert.That(logger.Warnings).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ADifferentMiss_LogsAgain()
    {
        using var root = new TempWebRoot();
        var manifest = Create(root, out var logger);

        _ = manifest.ContainsKey("Features/Fallback.ts");
        root.WriteManifest(WithoutEntry);
        _ = manifest.ContainsKey("Features/Fallback.ts");

        _ = await Assert.That(logger.Warnings).Count().IsEqualTo(2);
    }

    private static FallbackManifest Create(TempWebRoot root, out WarningLogger logger)
    {
        var environment = new Mock<IWebHostEnvironment>();
        _ = environment.SetupGet(e => e.WebRootPath).Returns(root.Path);
        logger = new WarningLogger();
        return new FallbackManifest(environment.Object, logger);
    }

    private sealed class WarningLogger : ILogger<FallbackManifest>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    private sealed class TempWebRoot : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("kcc-fallback-").FullName;

        public void WriteManifest(string json)
        {
            var folder = System.IO.Path.Combine(Path, "fallback", ".vite");
            _ = Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), json);
        }

        public void DeleteManifest() => File.Delete(System.IO.Path.Combine(Path, "fallback", ".vite", "manifest.json"));

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
