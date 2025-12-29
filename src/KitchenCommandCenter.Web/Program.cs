using Kentico.Activities.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using KitchenCommandCenter;
using KitchenCommandCenter.Web.Features.Cache;
using KitchenCommandCenter.Web.Features.Sitemap;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RobotsTxt;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<ICacheService, CacheService>();
builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();
builder.Services.AddViteServices(options =>
{
    options.Server.AutoRun = true;
    options.Server.PackageManager = "yarn";
});

builder.Services.AddAutoMapper(typeof(Program));

// Enable desired Kentico Xperience features
builder.Services.AddKentico(features =>
{
    features.UsePageBuilder(
        new PageBuilderOptions
        {
            RegisterDefaultSection = false,
            DefaultSectionIdentifier = "KitchenCommandCenter.BaseSection",
            ContentTypeNames = [HomePage.CONTENT_TYPE_NAME, PageBuilderPage.CONTENT_TYPE_NAME],
        }
    );
    features.UseWebPageRouting();
    features.UseActivityTracking();

    // features.UseEmailStatisticsLogging();
    // features.UseEmailMarketing();
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKenticoMiniProfiler();
}

builder.Services.AddAuthentication();

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.InitKentico();

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseAuthentication();

app.UseKentico();

app.UseRobotsTxt();

if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
    app.UseWebSockets();

    if (app.Configuration["ASPNETCORE_VITE"]?.ToLower() == "true")
    {
        app.UseViteDevelopmentServer(true);
    }
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.Kentico().MapRoutes();

app.MapControllerRoute(name: "error", pattern: "error/{0}");

app.Run();
