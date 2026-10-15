using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

// The backoffice sends its signed-in user's bearer token with every Management API request. An API user with client
// credentials gets the same kind of token without a browser, in whichever user group a test needs.
public sealed class BackofficeClient : IDisposable
{
    private const string Secret = "Integration-Client-Secret-2026";

    private static readonly (Guid UserGroupKey, string Name)[] SessionUsers =
    [
        (Constants.Security.AdminGroupKey, "administrator"),
        (Constants.Security.EditorGroupKey, "editor"),
        (Constants.Security.TranslatorGroupKey, "translator"),
    ];

    private readonly HttpClient client;

    private BackofficeClient(HttpClient client) => this.client = client;

    public static Task<BackofficeClient> AdministratorAsync(UmbracoSite site) => SignInAsync(site, ClientId("administrator"));

    public static Task<BackofficeClient> EditorAsync(UmbracoSite site) => SignInAsync(site, ClientId("editor"));

    public static Task<BackofficeClient> TranslatorAsync(UmbracoSite site) => SignInAsync(site, ClientId("translator"));

    public static async Task CreateSessionUsersAsync(UmbracoSite site)
    {
        foreach (var (userGroupKey, name) in SessionUsers)
        {
            await CreateApiUserAsync(site, userGroupKey, name);
        }
    }

    public static async Task<BackofficeClient> ForNewUserAsync(UmbracoSite site, Guid userGroupKey)
    {
        var name = Guid.NewGuid().ToString("N")[..12];
        await CreateApiUserAsync(site, userGroupKey, name);
        return await SignInAsync(site, ClientId(name));
    }

    public Task<HttpResponseMessage> GetAsync(string path) => client.GetAsync(path);

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null) => client.PostAsync(path, body is null ? null : JsonContent.Create(body));

    public Task<HttpResponseMessage> PutAsync(string path, object body) => client.PutAsJsonAsync(path, body);

    public Task<HttpResponseMessage> DeleteAsync(string path) => client.DeleteAsync(path);

    public async Task<JsonElement> GetJsonAsync(string path)
    {
        using var response = await client.GetAsync(path);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public void Dispose() => client.Dispose();

    private static string ClientId(string name) => $"umbraco-back-office-it-{name}";

    private static async Task CreateApiUserAsync(UmbracoSite site, Guid userGroupKey, string name)
    {
        using var scope = site.Services.CreateScope();
        var created = await scope.ServiceProvider.GetRequiredService<IUserService>().CreateAsync(
            Constants.Security.SuperUserKey,
            new UserCreateModel
            {
                Email = $"api-{name}@example.test",
                UserName = $"api-{name}@example.test",
                Name = $"API {name}",
                Kind = UserKind.Api,
                UserGroupKeys = new HashSet<Guid> { userGroupKey },
            },
            approveUser: true);
        if (!created.Success)
        {
            throw new InvalidOperationException($"Creating an API user failed: {created.Status}.");
        }

        var saved = await scope.ServiceProvider.GetRequiredService<IBackOfficeUserClientCredentialsManager>()
            .SaveAsync(created.Result.CreatedUser!.Key, ClientId(name), Secret);
        if (!saved.Success)
        {
            throw new InvalidOperationException($"Saving client credentials failed: {saved.Result}.");
        }
    }

    private static async Task<BackofficeClient> SignInAsync(UmbracoSite site, string clientId)
    {
        // Umbraco's token endpoint refuses plain HTTP while Umbraco:CMS:Global:UseHttps is on, as it is by default.
        var http = site.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var token = await http.PostAsync(
            "/umbraco/management/api/v1/security/back-office/token",
            new FormUrlEncodedContent([new("grant_type", "client_credentials"), new("client_id", clientId), new("client_secret", Secret)]));
        var body = await token.Content.ReadFromJsonAsync<JsonElement>();
        if (!token.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"The token endpoint answered {(int)token.StatusCode}: {body}");
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("access_token").GetString());
        return new BackofficeClient(http);
    }
}
