using System.Diagnostics;

namespace KCC.E2ETests.Config;

/// <summary>
/// Starts the KCC.Web app once for the E2E test session (unless something is already serving
/// <see cref="Constants.TESTING_DOMAIN"/>) and tears it down afterwards. Locally this runs
/// <c>dotnet run</c> against the web project; in CI (ASPNETCORE_ENVIRONMENT=CI) it uses the
/// pre-built Release output via the CI launch profile.
/// </summary>
public static class WebAppFixture
{
    private static Process? webAppProcess;

    [Before(Assembly)]
    public static async Task StartWebApp()
    {
        using var client = new HttpClient();

        if (await IsAlreadyRunning(client))
        {
            return;
        }

        var solutionRoot = FindSolutionRoot();
        var webProjectPath = Path.Combine(solutionRoot, "src", "KCC.Web", "KCC.Web.csproj");

        webAppProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = GetDotnetRunArguments(webProjectPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        _ = webAppProcess.Start();
        webAppProcess.BeginOutputReadLine();
        webAppProcess.BeginErrorReadLine();

        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(120);

        while (stopwatch.Elapsed < timeout)
        {
            if (webAppProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Web app process exited unexpectedly with code {webAppProcess.ExitCode}."
                );
            }

            try
            {
                _ = await client.GetAsync(Constants.TESTING_DOMAIN);

                return;
            }
            catch (HttpRequestException)
            {
                // App not ready yet
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Web app did not start within 120 seconds.");
    }

    [After(Assembly)]
    public static void StopWebApp()
    {
        if (webAppProcess is { HasExited: false })
        {
            webAppProcess.Kill(entireProcessTree: true);
        }

        webAppProcess?.Dispose();
    }

    private static async Task<bool> IsAlreadyRunning(HttpClient client)
    {
        try
        {
            _ = await client.GetAsync(Constants.TESTING_DOMAIN);

            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static string GetDotnetRunArguments(string webProjectPath)
    {
        var args = $"run --project \"{webProjectPath}\"";

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "CI")
        {
            args += " --launch-profile CI --no-build --configuration Release";
        }

        return args;
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("KitchenCommandCenter.sln").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find solution root (KitchenCommandCenter.sln).");
    }
}
