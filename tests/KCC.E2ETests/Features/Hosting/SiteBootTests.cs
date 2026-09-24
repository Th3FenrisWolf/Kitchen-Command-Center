using System.Text.Json;
using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Hosting;

public class SiteBootTests
{
    [ClassDataSource<SiteProcess>(Shared = SharedType.PerTestSession)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    public async Task FreshSite_ReportsTheRunLevel()
    {
        using var http = new HttpClient { BaseAddress = Site.BaseUrl };
        using var status = JsonDocument.Parse(await http.GetStringAsync("umbraco/management/api/v1/server/status"));

        _ = await Assert.That(status.RootElement.GetProperty("serverStatus").GetString()).IsEqualTo("Run");
    }
}
