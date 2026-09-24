using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using TUnit.Core.Interfaces;

namespace KCC.E2ETests.Config;

/// <summary>A KCC.Web process on a free port with its own SQLite file, started the way production starts it.</summary>
public sealed class SiteProcess : IAsyncInitializer, IAsyncDisposable
{
    private static readonly string Configuration =
        typeof(SiteProcess).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";

    private readonly bool withSsr;
    private readonly string imagingHmacSecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private Process? site;
    private Process? ssr;
    private string runDirectory = string.Empty;
    private int ssrPort;
    private TextWriter? log;

    public SiteProcess()
        : this(withSsr: true)
    {
    }

    public SiteProcess(bool withSsr) => this.withSsr = withSsr;

    public Uri BaseUrl { get; private set; } = null!;

    public string DatabasePath => Path.Combine(runDirectory, "Umbraco.sqlite.db");

    private string LogPath => Path.Combine(runDirectory, "site.log");

    public async Task InitializeAsync()
    {
        RequireBuildOutput();
        runDirectory = Directory.CreateTempSubdirectory("kcc-e2e-").FullName;
        Directory.CreateDirectory(Path.Combine(runDirectory, "tmp"));
        BaseUrl = new Uri($"http://127.0.0.1:{FreePort()}/");

        var writer = File.AppendText(LogPath);
        writer.AutoFlush = true;
        log = TextWriter.Synchronized(writer);

        if (withSsr)
        {
            ssrPort = FreePort();
            await StartSsrAsync();
        }

        await StartAsync();
    }

    public async Task StartAsync()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepoPaths.WebProject,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add(SiteDll);
        info.ArgumentList.Add("--urls");
        info.ArgumentList.Add(BaseUrl.GetLeftPart(UriPartial.Authority));
        foreach (var (key, value) in SiteEnvironment())
        {
            info.Environment[key] = value;
        }

        site = StartLogged(info);

        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(10) };
        for (var deadline = DateTime.UtcNow.AddMinutes(5); DateTime.UtcNow < deadline; await Task.Delay(1000))
        {
            if (site.HasExited)
            {
                throw new InvalidOperationException($"The site exited with {site.ExitCode}.{LogTail()}");
            }

            try
            {
                using var response = await http.GetAsync("umbraco/management/api/v1/server/status");
                if ((int)response.StatusCode >= 500)
                {
                    throw new InvalidOperationException($"Umbraco failed to boot.{LogTail()}");
                }

                using var status = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var level = status.RootElement.TryGetProperty("serverStatus", out var statusProperty) && statusProperty.ValueKind == JsonValueKind.String
                    ? statusProperty.GetString()
                    : null;
                if (level == "Run")
                {
                    return;
                }

                if (level is "Install" or "BootFailed")
                {
                    throw new InvalidOperationException($"Umbraco stopped at {level}.{LogTail()}");
                }
            }
            catch (HttpRequestException)
            {
                // Kestrel is not listening yet.
            }
            catch (TaskCanceledException)
            {
                // The first request after a fresh install can be slow.
            }
            catch (JsonException)
            {
                // A transient response before the management API is mapped is not JSON yet.
            }
        }

        throw new TimeoutException($"Umbraco did not reach Run within five minutes.{LogTail()}");
    }

    public async Task StopAsync()
    {
        if (site is { HasExited: false })
        {
            site.Kill(entireProcessTree: true);
            await site.WaitForExitAsync();
        }

        site?.Dispose();
        site = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (ssr is { HasExited: false })
        {
            ssr.Kill(entireProcessTree: true);
            await ssr.WaitForExitAsync();
        }

        ssr?.Dispose();
        log?.Dispose();
        log = null;
        if (runDirectory.Length > 0)
        {
            try
            {
                Directory.Delete(runDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A just-killed process can hold a file handle for a moment; the folder is disposable.
            }
        }
    }

    private static string SiteDll => Path.Combine(RepoPaths.WebProject, "bin", Configuration, "net10.0", "KCC.Web.dll");

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void RequireBuildOutput()
    {
        if (!File.Exists(SiteDll))
        {
            throw new InvalidOperationException($"{SiteDll} is missing. Build the solution first (`dotnet build`).");
        }

        var web = RepoPaths.WebProject;
        if (!File.Exists(Path.Combine(web, "wwwroot", ".vite", "manifest.json")) || !File.Exists(Path.Combine(web, "wwwroot", "ssr", "Server.Entry.js")))
        {
            throw new InvalidOperationException("The front-end bundles are missing. Run `yarn build:all` in src/KCC.Web first.");
        }
    }

    private async Task StartSsrAsync()
    {
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = RepoPaths.WebProject,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("Features/Ssr/Server.js");
        info.Environment["NODE_ENV"] = "production";
        info.Environment["SSR_PORT"] = ssrPort.ToString(CultureInfo.InvariantCulture);
        ssr = StartLogged(info);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var deadline = DateTime.UtcNow.AddSeconds(60); DateTime.UtcNow < deadline; await Task.Delay(500))
        {
            if (ssr.HasExited)
            {
                throw new InvalidOperationException($"The SSR service exited with {ssr.ExitCode}.{LogTail()}");
            }

            try
            {
                using var response = await http.GetAsync($"http://127.0.0.1:{ssrPort}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
        }

        throw new TimeoutException($"The SSR service did not answer /health within a minute.{LogTail()}");
    }

    private Process StartLogged(ProcessStartInfo info)
    {
        var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {info.FileName}.");
        process.OutputDataReceived += (_, e) => WriteLog(e.Data);
        process.ErrorDataReceived += (_, e) => WriteLog(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private void WriteLog(string? line)
    {
        try
        {
            log?.WriteLine(line);
        }
        catch (ObjectDisposedException)
        {
            // A redirected-stream callback can still fire briefly after DisposeAsync closes the writer.
        }
    }

    private string LogTail()
    {
        try
        {
            var lines = File.ReadAllLines(LogPath);
            return $"{Environment.NewLine}Last log lines ({LogPath}):{Environment.NewLine}{string.Join(Environment.NewLine, lines.TakeLast(40))}";
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private Dictionary<string, string> SiteEnvironment()
    {
        var temp = Path.Combine(runDirectory, "tmp");
        return new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Private;Foreign Keys=True;Pooling=True",
            ["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
            ["Umbraco__CMS__Unattended__InstallUnattended"] = "true",
            ["Umbraco__CMS__Unattended__UpgradeUnattended"] = "true",
            ["Umbraco__CMS__Unattended__UnattendedUserName"] = "E2E Admin",
            ["Umbraco__CMS__Unattended__UnattendedUserEmail"] = "admin@example.test",
            ["Umbraco__CMS__Unattended__UnattendedUserPassword"] = "E2E-Passw0rd-2026",
            ["Umbraco__CMS__Unattended__UnattendedTelemetryLevel"] = "Minimal",
            ["Umbraco__CMS__ModelsBuilder__ModelsMode"] = "Nothing",
            ["Umbraco__CMS__Hosting__LocalTempStorageLocation"] = "EnvironmentTemp",
            ["Umbraco__CMS__Hosting__SiteName"] = Path.GetFileName(runDirectory),
            ["Umbraco__CMS__Examine__LuceneDirectoryFactory"] = "TempFileSystemDirectoryFactory",
            ["Umbraco__CMS__Logging__Directory"] = Path.Combine(runDirectory, "logs"),
            ["Umbraco__CMS__WebRouting__UmbracoApplicationUrl"] = BaseUrl.ToString(),
            ["Umbraco__CMS__Imaging__HMACSecretKey"] = imagingHmacSecretKey,
            ["DataProtection__KeysDirectory"] = Path.Combine(runDirectory, "keys"),
            ["uSync__Settings__ExportOnSave"] = "None",
            ["VueSsr__Enabled"] = withSsr ? "true" : "false",
            ["VueSsr__BaseUrl"] = $"http://127.0.0.1:{ssrPort}",
            ["TMPDIR"] = temp,
            ["TMP"] = temp,
            ["TEMP"] = temp,
        };
    }
}
