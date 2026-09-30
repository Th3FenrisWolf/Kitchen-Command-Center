using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KCC.IntegrationTests.Config;

// One visitor's browser. It keeps its cookies, sends the anti-forgery token the layout hands out, and comes from its
// own address, so no test spends another test's rate limit.
public sealed partial class MemberClient : IDisposable
{
    // Any page rendered through the layout hands out a token, and this one renders signed in or out.
    private const string TokenPage = "/recipes/";

    private static int nextAddress;

    private readonly HttpClient client;
    private string token = string.Empty;

    public MemberClient(UmbracoSite site)
    {
        client = site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", $"2001:db8::{Interlocked.Increment(ref nextAddress):x}");
    }

    public HttpClient Http => client;

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null) => SendAsync(HttpMethod.Post, path, body);

    public Task<HttpResponseMessage> PutAsync(string path, object? body = null) => SendAsync(HttpMethod.Put, path, body);

    public Task<HttpResponseMessage> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path, null);

    public async Task<AuthResult> SignInAsync(string userName, string password, string? returnUrl = null)
    {
        using var response = await PostAsync("/api/account/login", new { userName, password, rememberMe = false, returnUrl });
        var result = await response.Content.ReadFromJsonAsync<AuthResult>() ?? throw new InvalidOperationException("Sign-in answered no body.");

        // The token belongs to the visitor it was issued to, so a new identity needs a new one.
        token = string.Empty;
        return result;
    }

    public async Task<HttpResponseMessage> SignOutAsync()
    {
        await EnsureTokenAsync();
        using var form = new FormUrlEncodedContent([new("__RequestVerificationToken", token)]);
        var response = await client.PostAsync("/account/logout", form);
        token = string.Empty;
        return response;
    }

    public async Task<bool> IsSignedInAsync(string variantPath)
    {
        var page = await RenderedPage.GetAsync(client, variantPath);
        return page.Attribute(":is-authenticated") == "true";
    }

    public void Dispose() => client.Dispose();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body)
    {
        await EnsureTokenAsync();
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("RequestVerificationToken", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private async Task EnsureTokenAsync()
    {
        if (token.Length > 0)
        {
            return;
        }

        var html = await client.GetStringAsync(TokenPage);
        var config = ApiConfig().Match(html);
        if (!config.Success)
        {
            throw new InvalidOperationException($"{TokenPage} carries no api-config block.");
        }

        using var json = JsonDocument.Parse(config.Groups[1].Value);
        token = json.RootElement.GetProperty("antiforgeryToken").GetString() ?? string.Empty;
    }

    [GeneratedRegex("<script type=\"application/json\" id=\"api-config\">(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ApiConfig();
}

public sealed record AuthResult(bool Success, string[]? Errors, string? RedirectUrl);
