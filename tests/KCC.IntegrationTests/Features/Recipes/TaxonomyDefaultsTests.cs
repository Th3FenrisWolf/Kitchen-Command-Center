using KCC.IntegrationTests.Config;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Recipes;

public class TaxonomyDefaultsTests
{
    private static readonly TaxonomyDefault Keto = TaxonomyDefaults.Values.Single(value => value.Name == "Keto");
    private static readonly TaxonomyDefault Dinner = TaxonomyDefaults.Values.Single(value => value.Name == "Dinner");

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Startup_RecordsThatTheDefaultsRan()
    {
        var applied = Site.Services.GetRequiredService<IKeyValueService>().GetValue(TaxonomyDefaultsOnStartup.AppliedKey);

        _ = await Assert.That(applied).IsNotNull();
    }

    [Test]
    public async Task Startup_LeavesAClearedIconAloneOnceTheDefaultsRan()
    {
        try
        {
            await ClearAsync(Dinner, publish: true);
            var startup = ActivatorUtilities.CreateInstance<TaxonomyDefaultsOnStartup>(Site.Services);

            await startup.HandleAsync(new UmbracoApplicationStartedNotification(false), CancellationToken.None);

            _ = await Assert.That(Saved(Dinner)).IsNullOrEmpty();
        }
        finally
        {
            await ApplyAsync();
        }
    }

    [Test]
    public async Task Apply_FillsAMissingKindAndIcon_ThenLeavesThemAlone()
    {
        try
        {
            await ClearAsync(Keto, publish: false);
            await ClearAsync(Dinner, publish: true);
            _ = await Assert.That(HasUnpublishedChanges(Keto)).IsTrue();
            _ = await Assert.That(await ApplyAsync()).IsEqualTo(2);
            _ = await Assert.That(await ApplyAsync()).IsEqualTo(0);
            _ = await Assert.That(Saved(Keto)).IsEqualTo("[\"Diet\"]");
            _ = await Assert.That(HasUnpublishedChanges(Keto)).IsFalse();
            _ = await Assert.That(Published<RecipeTag>(Keto, tag => tag.Kind)).IsEqualTo(TagKinds.Diet);
            _ = await Assert.That(Published<RecipeCategory>(Dinner, category => category.Icon)).IsEqualTo("fa-duotone fa-pot-food");
        }
        finally
        {
            await ApplyAsync();
        }
    }

    private async Task<int> ApplyAsync()
    {
        using var scope = Site.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<TaxonomyDefaults>().ApplyAsync();
    }

    private async Task ClearAsync(TaxonomyDefault value, bool publish)
    {
        using var scope = Site.Services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
        var content = contentService.GetById(value.Key)!;
        content.SetValue(value.Alias, null);
        _ = await Assert.That(contentService.Save(content).Success).IsTrue();
        if (publish)
        {
            var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
                .PublishAsync(value.Key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
            _ = await Assert.That(published.Success).IsTrue();
        }
    }

    private string? Saved(TaxonomyDefault value) =>
        Site.Services.GetRequiredService<IContentService>().GetById(value.Key)!.GetValue<string>(value.Alias);

    private bool HasUnpublishedChanges(TaxonomyDefault value) =>
        Site.Services.GetRequiredService<IContentService>().GetById(value.Key)!.Edited;

    private string? Published<T>(TaxonomyDefault value, Func<T, string?> read)
        where T : class
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        return read((T)context.UmbracoContext.Content!.GetById(value.Key)!);
    }
}
