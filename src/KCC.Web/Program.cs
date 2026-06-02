using KCC;
using KCC.ResourceStrings;
using KCC.Web.Features.AdminHomePage;
using KCC.Web.Features.Attributes;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Sitemap;
using KCC.Web.Features.Ssr;
using Kentico.Activities.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Membership;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Xperience.ComponentRegistry;
using Kentico.Xperience.ManagementApi;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using RobotsTxt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddVueSsr(builder.Configuration);

builder.Services.AddKentico(features =>
{
    features.UseWebPageRouting(new() { LanguageNameRouteValuesKey = "lang" });
    features.UseActivityTracking();
    features.UsePageBuilder(
        new()
        {
            RegisterDefaultSection = false,
            DefaultSectionIdentifier = "KCC.BaseSection",
            ContentTypeNames = [
                HomePage.CONTENT_TYPE_NAME,
                PageBuilderPage.CONTENT_TYPE_NAME,
            ],
        }
    );
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKenticoMiniProfiler();

    builder.Services.AddKenticoManagementApi(options =>
    {
        options.Secret = builder.Configuration["KenticoManagementApi:Secret"];
    });

    builder.Services.AddComponentRegistry();
    builder.Services
        .AddComponentRegistryMcpServices()
        .AddMcpServer()
        .WithHttpTransport()
        .WithComponentRegistryTools();
}

builder.Services.AddAuthentication();
builder.Services.AddIdentity<KCCApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
.AddUserStore<ApplicationUserStore<KCCApplicationUser>>()
.AddRoleStore<ApplicationRoleStore<ApplicationRole>>()
.AddUserManager<UserManager<KCCApplicationUser>>()
.AddSignInManager<SignInManager<KCCApplicationUser>>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddKccResourceStrings();

var anthropicOptions = builder.Configuration
    .GetSection(KCC.Web.Features.Api.AnthropicOptions.SectionName)
    .Get<KCC.Web.Features.Api.AnthropicOptions>() ?? new KCC.Web.Features.Api.AnthropicOptions();
builder.Services.AddSingleton(anthropicOptions);
builder.Services.AddSingleton(new Anthropic.AnthropicClient(new Anthropic.Core.ClientOptions { ApiKey = anthropicOptions.ApiKey ?? string.Empty }));
builder.Services.AddSingleton<KCC.Admin.IRecipeIconService, KCC.Web.Features.Api.RecipeIconService>();

builder.Services.AddControllersWithViews(options =>
{
    var localizedRouteConvention = new LocalizedRouteConvention();
    options.Conventions.Add((IControllerModelConvention)localizedRouteConvention);
    options.Conventions.Add((IActionModelConvention)localizedRouteConvention);
})
.AddApplicationPart(typeof(ResourceStringServiceExtensions).Assembly);

builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();

var app = builder.Build();

app.InitKentico();

app.UseStaticFiles();
app.UseCookiePolicy();

app.UseAuthentication();

app.UseAdminHomePageRedirect();

// Must be registered before UseKentico so that in the response phase it
// runs after Kentico's virtual-context decorator has rewritten URLs.
app.UseMiddleware<PreviewJsonUrlSyncMiddleware>();
app.UseKentico();
app.UseVueSsr();

// Must be between UseKentico (which calls UseRouting) and the endpoint mappings
// below so [Authorize] attributes — e.g., on ResourceStringEditorController — fire.
app.UseAuthorization();

app.UseRobotsTxt();

if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

// Restore the lang route value lost during status code re-execute so that
// IPreferredLanguageRetriever, content retrieval, and URL generation all
// resolve the correct language for the original request.
app.Use(async (context, next) =>
{
    var reExecuteFeature = context.Features.Get<IStatusCodeReExecuteFeature>();
    if (reExecuteFeature?.OriginalPath is string originalPath
        && !context.Request.RouteValues.ContainsKey("lang"))
    {
        var segments = originalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && segments[0].Length == 2)
        {
            context.Request.RouteValues["lang"] = segments[0];
        }
    }

    await next();
});

app.Kentico().MapRoutes();
app.MapControllers();
app.MapControllerRoute(name: "error", pattern: "{lang}/error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{lang}/{controller}/{action}/{id?}");

app.Run();
