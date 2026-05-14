using KCC;
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
                PageBuilderPage.CONTENT_TYPE_NAME
            ],
        }
    );
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKenticoMiniProfiler();
}

builder.Services.AddAuthentication();
builder.Services.AddIdentity<KCCApplicationUser, NoOpApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
.AddUserStore<ApplicationUserStore<KCCApplicationUser>>()
.AddRoleStore<NoOpApplicationRoleStore>()
.AddUserManager<UserManager<KCCApplicationUser>>()
.AddSignInManager<SignInManager<KCCApplicationUser>>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(options =>
{
    var localizedRouteConvention = new LocalizedRouteConvention();
    options.Conventions.Add((IControllerModelConvention)localizedRouteConvention);
    options.Conventions.Add((IActionModelConvention)localizedRouteConvention);
});
builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();

var app = builder.Build();

app.InitKentico();

app.UseStaticFiles();
app.UseCookiePolicy();

app.UseAuthentication();

app.UseAdminHomePageRedirect();

app.UseKentico();
app.UseVueSsr();

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

app.Kentico().MapRoutes();
app.MapControllerRoute(name: "error", pattern: "{lang}/error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{lang}/{controller}/{action}/{id?}");

app.Run();
