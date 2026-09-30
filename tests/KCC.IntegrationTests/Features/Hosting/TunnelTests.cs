using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Hosting;

public class TunnelTests
{
    private const string Authorize = "/umbraco/management/api/v1/security/back-office/authorize";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    // The backoffice's sign-in refuses plain HTTP (OpenIddict ID2083) while UseHttps is on, as production requires.
    // ID2029, the missing client_id, is the next check it makes, so it took the request as HTTPS.
    [Test]
    public async Task BackofficeSignIn_ForwardedAsHttpsByTheTunnel_IsTakenAsHttps()
    {
        var body = await AuthorizeAsync(UmbracoSite.TunnelAddress);

        _ = await Assert.That(body).Contains("ID2029");
    }

    [Test]
    [Arguments("192.0.2.99")]
    [Arguments("127.0.0.1")]
    [Arguments("::1")]
    public async Task BackofficeSignIn_ForwardedAsHttpsByAnyoneElse_IsRefused(string peer)
    {
        var body = await AuthorizeAsync(peer);

        _ = await Assert.That(body).Contains("ID2083");
    }

    [Test]
    public async Task HttpsResponses_CarryStrictTransportSecurity()
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        request.Headers.Host = "kcc.example.test";
        request.Headers.Add(UmbracoSite.PeerHeader, UmbracoSite.TunnelAddress);
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);

        _ = await Assert.That(response.Headers.Contains("Strict-Transport-Security")).IsTrue();
    }

    [Test]
    public async Task PlainHttpResponses_CarryNoStrictTransportSecurity()
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        request.Headers.Host = "kcc.example.test";
        using var response = await client.SendAsync(request);

        _ = await Assert.That(response.Headers.Contains("Strict-Transport-Security")).IsFalse();
    }

    private async Task<string> AuthorizeAsync(string peer)
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Authorize);
        request.Headers.Add(UmbracoSite.PeerHeader, peer);
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
