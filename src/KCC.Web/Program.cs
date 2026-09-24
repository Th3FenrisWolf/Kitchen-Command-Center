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

// Umbraco never persists data-protection keys, so without this every restart signs everyone out and
// invalidates the anti-forgery tokens in open forms.
builder.Services.AddDataProtection()
    .SetApplicationName("kcc")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysDirectory"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "keys")));

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
