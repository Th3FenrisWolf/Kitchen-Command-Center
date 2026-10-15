using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KCC.IntegrationTests.Features.Pages;

public class RecipeImportPagePropsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [After(Test)]
    public void ResetTheReader() => Site.PhotoReader.Reset();

    [Test]
    public async Task WhileTheFakeDelegates_BothWizardPagesRenderTheImportDisabled()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Kinkajou Off Crumble");
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        foreach (var import in await PhotoImportPropsAsync(member, recipe))
        {
            _ = await Assert.That(import.GetProperty("enabled").GetBoolean()).IsFalse();
            _ = await Assert.That(import.GetProperty("available").GetBoolean()).IsFalse();
            _ = await Assert.That(import.GetProperty("maxImageEdge").GetInt32()).IsEqualTo(ExpectedMaxImageEdge());
        }
    }

    [Test]
    public async Task OnceTheFakeIsSwitchedOn_BothWizardPagesRenderTheImportEnabledAndAvailable()
    {
        Site.PhotoReader.SwitchOn();
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Kinkajou On Crumble");
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        foreach (var import in await PhotoImportPropsAsync(member, recipe))
        {
            _ = await Assert.That(import.GetProperty("enabled").GetBoolean()).IsTrue();
            _ = await Assert.That(import.GetProperty("available").GetBoolean()).IsTrue();
            _ = await Assert.That(import.TryGetProperty("availableAgainOn", out _)).IsFalse();
            _ = await Assert.That(import.GetProperty("maxImageEdge").GetInt32()).IsEqualTo(ExpectedMaxImageEdge());
        }
    }

    private static async Task<JsonElement[]> PhotoImportPropsAsync(SignedInMember member, Guid recipe)
    {
        var create = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/create-recipe/");
        var add = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={recipe}");
        return [create.Prop("photo-import"), add.Prop("photo-import")];
    }

    private int ExpectedMaxImageEdge()
    {
        var options = Site.Services.GetRequiredService<IOptionsMonitor<RecipeImportOptions>>().CurrentValue;
        return ModelProfiles.Find(options.Model) is { } profile
            ? Math.Min(options.MaxImageEdge, profile.MaxImageEdge)
            : options.MaxImageEdge;
    }
}
