namespace KCC.Tests;

// Each test site writes its logs into a temp folder that its fixture deletes, so a CI-only failure leaves nothing to
// read. CI names a folder in KCC_TEST_SITE_LOGS and uploads it when a test fails.
internal static class SiteLogs
{
    public static string Destination => Environment.GetEnvironmentVariable("KCC_TEST_SITE_LOGS") ?? string.Empty;

    public static void Keep(string runDirectory, string destination)
    {
        if (destination.Length == 0 || !Directory.Exists(runDirectory))
        {
            return;
        }

        var copy = Path.Combine(destination, Path.GetFileName(runDirectory));
        foreach (var log in Directory.EnumerateFiles(runDirectory, "*.log"))
        {
            CopyInto(copy, log, runDirectory);
        }

        var logs = Path.Combine(runDirectory, "logs");
        if (Directory.Exists(logs))
        {
            foreach (var log in Directory.EnumerateFiles(logs, "*", SearchOption.AllDirectories))
            {
                CopyInto(copy, log, runDirectory);
            }
        }
    }

    private static void CopyInto(string copy, string file, string runDirectory)
    {
        var target = Path.Combine(copy, Path.GetRelativePath(runDirectory, file));
        _ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(file, target, overwrite: true);
    }
}
