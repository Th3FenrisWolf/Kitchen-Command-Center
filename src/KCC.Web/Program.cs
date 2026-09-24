using KCC.Web.Features.Ssr;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.Services.AddMemoryCache();
builder.Services.AddVueSsr(builder.Configuration);

// Umbraco never persists data-protection keys, so without this every restart signs everyone out and
// invalidates the anti-forgery tokens in open forms.
builder.Services.AddDataProtection()
    .SetApplicationName("kcc")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysDirectory"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "keys")));

var app = builder.Build();

await app.BootUmbracoAsync();

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
