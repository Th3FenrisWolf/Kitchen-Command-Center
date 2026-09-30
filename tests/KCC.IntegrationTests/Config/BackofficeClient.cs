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

    private readonly HttpClient client;

    private BackofficeClient(HttpClient client) => this.client = client;

    public static Task<BackofficeClient> AdministratorAsync(UmbracoSite site) => InGroupAsync(site, Constants.Security.AdminGroupKey);

    public static Task<BackofficeClient> EditorAsync(UmbracoSite site) => InGroupAsync(site, Constants.Security.EditorGroupKey);

    public static Task<BackofficeClient> TranslatorAsync(UmbracoSite site) => InGroupAsync(site, Constants.Security.TranslatorGroupKey);

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

    private static async Task<BackofficeClient> InGroupAsync(UmbracoSite site, Guid userGroupKey)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var clientId = $"umbraco-back-office-it-{id}";
        using (var scope = site.Services.CreateScope())
        {
            var created = await scope.ServiceProvider.GetRequiredService<IUserService>().CreateAsync(
                Constants.Security.SuperUserKey,
                new UserCreateModel
                {
                    Email = $"api-{id}@example.test",
                    UserName = $"api-{id}@example.test",
                    Name = $"API {id}",
                    Kind = UserKind.Api,
                    UserGroupKeys = new HashSet<Guid> { userGroupKey },
                },
                approveUser: true);
            if (!created.Success)
            {
                throw new InvalidOperationException($"Creating an API user failed: {created.Status}.");
            }

            var saved = await scope.ServiceProvider.GetRequiredService<IBackOfficeUserClientCredentialsManager>()
                .SaveAsync(created.Result.CreatedUser!.Key, clientId, Secret);
            if (!saved.Success)
            {
                throw new InvalidOperationException($"Saving client credentials failed: {saved.Result}.");
            }
        }

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
