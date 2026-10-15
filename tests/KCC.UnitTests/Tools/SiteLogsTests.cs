using KCC.Tests;

namespace KCC.UnitTests.Tools;

public class SiteLogsTests
{
    [Test]
    public async Task Keep_CopiesTheLogsOfTheRun_IntoAFolderNamedAfterIt()
    {
        var run = Directory.CreateTempSubdirectory("kcc-it-");
        var kept = Directory.CreateTempSubdirectory("kcc-logs-");
        try
        {
            Directory.CreateDirectory(Path.Combine(run.FullName, "logs"));
            await File.WriteAllTextAsync(Path.Combine(run.FullName, "logs", "UmbracoTraceLog.json"), "trace");
            await File.WriteAllTextAsync(Path.Combine(run.FullName, "site.log"), "stdout");
            await File.WriteAllTextAsync(Path.Combine(run.FullName, "Umbraco.sqlite.db"), "data");

            SiteLogs.Keep(run.FullName, kept.FullName);

            var copy = Path.Combine(kept.FullName, run.Name);
            _ = await Assert.That(await File.ReadAllTextAsync(Path.Combine(copy, "logs", "UmbracoTraceLog.json"))).IsEqualTo("trace");
            _ = await Assert.That(await File.ReadAllTextAsync(Path.Combine(copy, "site.log"))).IsEqualTo("stdout");
            _ = await Assert.That(File.Exists(Path.Combine(copy, "Umbraco.sqlite.db"))).IsFalse();
        }
        finally
        {
            run.Delete(recursive: true);
            kept.Delete(recursive: true);
        }
    }

    [Test]
    public async Task Keep_WithNoDestination_CopiesNothing()
    {
        var run = Directory.CreateTempSubdirectory("kcc-it-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(run.FullName, "site.log"), "stdout");

            SiteLogs.Keep(run.FullName, destination: string.Empty);

            _ = await Assert.That(Directory.GetFileSystemEntries(run.FullName).Length).IsEqualTo(1);
        }
        finally
        {
            run.Delete(recursive: true);
        }
    }
}
