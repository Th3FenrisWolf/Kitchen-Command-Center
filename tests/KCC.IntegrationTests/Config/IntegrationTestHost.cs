using KCC.Web.Features.Members;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KCC.IntegrationTests.Config;

/// <summary>
/// Boots a single <see cref="WebApplication"/> for the lifetime of the TUnit test session.
/// Mirrors the relevant <c>Program.cs</c> service registrations so tests run against the real
/// Kentico DI graph, but never starts the HTTP listener.
/// </summary>
public static class IntegrationTestHost
{
    private static readonly object BuildLock = new();
    private static WebApplication? application;

    public static IServiceProvider Services => GetApplication().Services;

    // Initialization is lazy — the first call to Services (from TestsDIAttribute resolution)
    // builds the host. We intentionally skip disposal: Kentico registers an
    // AppDomain.ProcessExit handler (CMSApplication.ApplicationEnd) that resolves services
    // from the IoC container, and disposing the container before process exit throws
    // IoCContainerDisposedException during shutdown. The process is ending anyway.

    private static WebApplication GetApplication()
    {
        if (application is not null)
        {
            return application;
        }

        lock (BuildLock)
        {
            if (application is not null)
            {
                return application;
            }

            application = BuildApplication();
            application.InitKentico();
        }

        return application;
    }

    private static WebApplication BuildApplication()
    {
        // NOTE: Intentionally registers only the services needed to resolve the system under
        // test against the real Kentico DI graph. Items tied to the HTTP pipeline, identity,
        // SSR, the admin UI, or request-scoped middleware are intentionally omitted — tests
        // resolve services directly from DI.
        //
        // Args = [] here stops WebApplication from layering the process command line into
        // its IConfiguration; ModuleInit.ClearCommandLineArgsCache handles the parallel
        // problem for Kentico, which reads Environment.GetCommandLineArgs() directly.
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, Args = [] }
        );

        // Stdout is reserved for Microsoft.Testing.Platform's JSON-RPC protocol under
        // `dotnet test`. Any framework log lines would corrupt it and break reporting.
        _ = builder.Logging.ClearProviders();

        // Only load user secrets for local development. In CI, secrets.json may exist with
        // stale/empty values that would override appsettings.CI.json.
        if (!string.Equals(builder.Environment.EnvironmentName, "CI", StringComparison.OrdinalIgnoreCase))
        {
            _ = builder.Configuration.AddUserSecrets<TestsDIAttribute>(optional: true);
        }

        _ = builder.Services.AddScoped<IAuthorNameResolver, AuthorNameResolver>();

        _ = builder.Services.AddKentico();

        return builder.Build();
    }
}
