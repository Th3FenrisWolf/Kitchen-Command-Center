using KCC.IntegrationTests.Config;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Components;

public class BreadcrumbServiceTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Build_RunsFromHomeToTheVariant()
    {
        using var scope = Site.Services.CreateScope();
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        var variant = context.UmbracoContext.Content!.GetById(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack"))!;

        var trail = scope.ServiceProvider.GetRequiredService<BreadcrumbService>().Build(variant);

        _ = await Assert.That(string.Join("|", trail.Select(link => $"{link.LinkText}>{link.Url}")))
            .IsEqualTo("Home>/|Recipes>/recipes/|Fluffy Buttermilk Pancakes>/recipes/fluffy-buttermilk-pancakes/|Classic Stack>");
    }
}
