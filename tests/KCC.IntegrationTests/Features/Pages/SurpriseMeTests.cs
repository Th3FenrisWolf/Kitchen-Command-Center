using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Pages;

public class SurpriseMeTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SurpriseMe_LandsOnAPublishedRecipe()
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        using var client = Site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        for (var visit = 0; visit < 5; visit++)
        {
            using var response = await client.GetAsync("/surprise-me");
            var recipe = response.Headers.Location!.OriginalString;
            using var page = await client.GetAsync(recipe);

            _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
            _ = await Assert.That(recipe).StartsWith("/recipes/");
            _ = await Assert.That(recipe.Length).IsGreaterThan("/recipes/".Length);
            _ = await Assert.That(page.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }

    [Test]
    public async Task NoPage_TakesTheSurpriseMeAddress()
    {
        var urls = Site.Services.GetRequiredService<IDocumentUrlService>();

        _ = await Assert.That(urls.GetDocumentKeyByRoute("/recipes", null, null, false)).IsNotNull();
        _ = await Assert.That(urls.GetDocumentKeyByRoute("/surprise-me", null, null, false)).IsNull();
    }
}
