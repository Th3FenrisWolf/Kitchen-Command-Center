using KCC.Web.Features.Components.Header;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Sitemap;
using KCC.Web.Features.Ssr;
using Microsoft.AspNetCore.DataProtection;
using RobotsTxt;

var builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.Services.AddMemoryCache();
builder.Services.AddVueSsr(builder.Configuration);
builder.Services.AddScoped<IResourceStringProvider, DictionaryResourceStringProvider>();
builder.Services.AddScoped<ISiteSettingsQueries, SiteSettingsQueries>();
builder.Services.AddScoped<PageMetadata>();
builder.Services.AddScoped<ISitemapPages, SitemapPages>();
builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();

// ASP.NET Core keeps data-protection keys in the user profile when it can, but only in memory where the home
// directory isn't writable (as in the container), where every restart would sign everyone out and invalidate the
// anti-forgery tokens in open forms. An unset Compose variable arrives as an empty string rather than a missing one.
var keysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
builder.Services.AddDataProtection()
    .SetApplicationName("kcc")
    .PersistKeysToFileSystem(new DirectoryInfo(
        string.IsNullOrWhiteSpace(keysDirectory)
            ? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "keys")
            : keysDirectory));

var app = builder.Build();

await app.BootUmbracoAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseRobotsTxt();
app.UseVueSsr();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
