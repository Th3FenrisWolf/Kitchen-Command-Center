using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TUnit.Core.Interfaces;

namespace KCC.IntegrationTests.Config;

// The test host renders with SSR off, so a test that needs the rendered markup posts the page's own regions to the
// production SSR bundle, as the layout would.
public sealed class SsrSidecar : IAsyncInitializer, IAsyncDisposable
{
    private readonly StringBuilder output = new();
    private Process? process;
    private Uri baseUrl = null!;

    public async Task InitializeAsync()
    {
        var bundle = Path.Combine(RepoPaths.WebProject, "wwwroot", "ssr", "Server.Entry.js");
        if (!File.Exists(bundle))
        {
            throw new InvalidOperationException("The SSR bundle is missing. Run `yarn build:all` at the repository root first.");
        }

        var port = FreePort();
        baseUrl = new Uri($"http://127.0.0.1:{port}/");
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = RepoPaths.WebProject,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("Features/Ssr/Server.js");
        info.Environment["NODE_ENV"] = "production";
        info.Environment["SSR_PORT"] = port.ToString(CultureInfo.InvariantCulture);

        process = Process.Start(info) ?? throw new InvalidOperationException("Could not start node.");
        process.OutputDataReceived += (_, e) => Keep(e.Data);
        process.ErrorDataReceived += (_, e) => Keep(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            using var http = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(5) };
            for (var deadline = DateTime.UtcNow.AddSeconds(60); DateTime.UtcNow < deadline; await Task.Delay(250))
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException($"The SSR service exited with {process.ExitCode}.{Environment.NewLine}{Output()}");
                }

                try
                {
                    using var response = await http.GetAsync("health");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    // Not listening yet.
                }
            }

            throw new TimeoutException($"The SSR service did not answer /health within a minute.{Environment.NewLine}{Output()}");
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (process is { HasExited: false })
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process?.Dispose();
    }

    public async Task<SsrRender> RenderAsync(string headerContent, string bodyContent, string footerContent, uint seed, string page)
    {
        using var http = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };
        using var response = await http.PostAsJsonAsync(
            "render",
            new { headerContent, bodyContent, footerContent, isPreview = false, bloom = new { seed, page } });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"The SSR service answered {(int)response.StatusCode}: {body}{Environment.NewLine}{Output()}");
        }

        using var json = JsonDocument.Parse(body);
        return new SsrRender(
            json.RootElement.GetProperty("html").GetString() ?? string.Empty,
            json.RootElement.GetProperty("schedule").GetString() ?? string.Empty);
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void Keep(string? line)
    {
        lock (output)
        {
            output.AppendLine(line);
        }
    }

    private string Output()
    {
        lock (output)
        {
            return output.ToString();
        }
    }
}

public sealed record SsrRender(string Html, string Schedule);
