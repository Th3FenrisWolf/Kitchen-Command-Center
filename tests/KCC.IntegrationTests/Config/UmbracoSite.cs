using Examine.Lucene.Directories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using TUnit.Core.Interfaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace KCC.IntegrationTests.Config;

public sealed class UmbracoSite : WebApplicationFactory<Program>, IAsyncInitializer
{
    // The file extension keeps Umbraco's content routing off this path, so the request reaches the end of the pipeline.
    public const string ThrowingPath = "/integration-tests-throw.txt";

    private readonly string imagingHmacSecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private string runDirectory = string.Empty;
    private string localTempPath = string.Empty;
    private string examineTempPath = string.Empty;
    private string mediaCachePath = string.Empty;

    public string DatabasePath => Path.Combine(runDirectory, "Umbraco.sqlite.db");

    private string MediaCacheFolderName => $"{Path.GetFileName(runDirectory)}-media-cache";

    public async Task InitializeAsync()
    {
        RequireFrontEndBuild();
        runDirectory = Directory.CreateTempSubdirectory("kcc-it-").FullName;

        // The factory builds its host synchronously on first access; TUnit can touch it from parallel tests.
        await Task.Run(() => _ = Server);

        var state = Services.GetRequiredService<IRuntimeState>();
        for (var attempt = 0; state.Level == RuntimeLevel.Upgrading && attempt < 600; attempt++)
        {
            await Task.Delay(100);
        }

        if (state.Level != RuntimeLevel.Run)
        {
            throw new InvalidOperationException($"Umbraco stopped at {state.Level} ({state.Reason}).", state.BootFailedException);
        }

        // LocalTempStorageLocation=EnvironmentTemp and LuceneDirectoryFactory=TempFileSystemDirectoryFactory each
        // hash SiteName into a folder name under Path.GetTempPath(), independently of runDirectory and of each
        // other; capturing the real, booted values is the only way to clean them up without reimplementing
        // Umbraco's own hashing.
        var hostingEnvironment = Services.GetRequiredService<Umbraco.Cms.Core.Hosting.IHostingEnvironment>();
        localTempPath = hostingEnvironment.LocalTempPath;
        examineTempPath = UmbracoTempEnvFileSystemDirectoryFactory.GetTempPath(
            Services.GetRequiredService<IApplicationIdentifier>(),
            hostingEnvironment);
        mediaCachePath = Path.Combine(WebProjectDirectory(), "umbraco", "Data", "TEMP", MediaCacheFolderName);

        await SeedTestRecipesAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        DeleteIfPresent(runDirectory);
        DeleteIfPresent(localTempPath);
        DeleteIfPresent(examineTempPath);
        DeleteIfPresent(mediaCachePath);
    }

    private static void DeleteIfPresent(string path)
    {
        if (path.Length == 0)
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // A log file can still be flushing; the temp folder is disposable either way.
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in Settings())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, ThrowingPathFilter>());

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<PublishedCacheProbe>();
            services.AddSingleton<INotificationHandler<ContentCacheRefresherNotification>>(
                provider => provider.GetRequiredService<PublishedCacheProbe>());
            services.AddSingleton<TrashRelationProbe>();
            services.AddSingleton<INotificationHandler<RelationSavingNotification>>(
                provider => provider.GetRequiredService<TrashRelationProbe>());
        });
    }

    private static void RequireFrontEndBuild()
    {
        var manifest = Path.Combine(WebProjectDirectory(), "wwwroot", ".vite", "manifest.json");
        if (!File.Exists(manifest))
        {
            throw new InvalidOperationException("Integration tests render real pages, which need the Vite manifest. Run `yarn build:all` in src/KCC.Web first.");
        }
    }

    private static string WebProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), "src", "KCC.Web");
    }

    // Seeding saves content and members, which must not overlap a test's writes (see AssemblyInfo.cs), so it
    // runs once, before any test.
    private async Task SeedTestRecipesAsync()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        using var response = await client.PostAsync("/api/dev/seed-recipes", null);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Seeding answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }

    private Dictionary<string, string> Settings() => new()
    {
        ["ConnectionStrings:umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Private;Foreign Keys=True;Pooling=True",
        ["ConnectionStrings:umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
        ["Umbraco:CMS:Unattended:InstallUnattended"] = "true",
        ["Umbraco:CMS:Unattended:UpgradeUnattended"] = "true",
        ["Umbraco:CMS:Unattended:UnattendedUserName"] = "Integration Admin",
        ["Umbraco:CMS:Unattended:UnattendedUserEmail"] = "admin@example.test",
        ["Umbraco:CMS:Unattended:UnattendedUserPassword"] = "Integration-Passw0rd-2026",
        ["Umbraco:CMS:Unattended:UnattendedTelemetryLevel"] = "Minimal",
        ["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing",
        ["Umbraco:CMS:Hosting:LocalTempStorageLocation"] = "EnvironmentTemp",
        ["Umbraco:CMS:Hosting:SiteName"] = Path.GetFileName(runDirectory),
        ["Umbraco:CMS:Examine:LuceneDirectoryFactory"] = "TempFileSystemDirectoryFactory",
        ["Umbraco:CMS:Logging:Directory"] = Path.Combine(runDirectory, "logs"),
        ["Umbraco:CMS:Imaging:HMACSecretKey"] = imagingHmacSecretKey,
        ["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = Path.Combine(runDirectory, "media"),

        // ImageSharp maps its cache folder under the content root even when given an absolute path.
        ["Umbraco:CMS:Imaging:Cache:CacheFolder"] = $"~/umbraco/Data/TEMP/{MediaCacheFolderName}",
        ["DataProtection:KeysDirectory"] = Path.Combine(runDirectory, "keys"),
        ["uSync:Settings:ExportOnSave"] = "None",
        ["VueSsr:Enabled"] = "false",

        // Tests wait for each rebuild, so a short quiet period keeps the suite quick.
        ["RecipeSearch:RebuildDelay"] = "00:00:00.100",

        // The seeder creates the approved member the E2E suite signs in as, from these two settings.
        ["KCC_E2E_MEMBER_USERNAME"] = "e2e-member",
        ["KCC_E2E_MEMBER_PASSWORD"] = "E2E-Member-Passw0rd",
    };

    // Appended after the app's own middleware, so an exception thrown here has to pass through its exception handler.
    private sealed class ThrowingPathFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path == ThrowingPath)
                {
                    throw new InvalidOperationException("Thrown on purpose by the integration test host.");
                }

                await nextMiddleware(context);
            });
        };
    }
}
