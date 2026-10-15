using KCC.Web.Features.DevTools.RecipeImport;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace KCC.Web.Features.RecipeImport;

public class RecipeImportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<RecipeImportOptions>(builder.Config.GetSection(RecipeImportOptions.SectionName));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IImportSpendLedger, ImportSpendLedger>();
        builder.Services.AddSingleton<IRecipePhotoReader>(PhotoReader);
        builder.Services.AddSingleton<ImportAvailability>();
    }

    private static IRecipePhotoReader PhotoReader(IServiceProvider services) =>
        UsesStandInReader(
            services.GetRequiredService<IHostEnvironment>(),
            services.GetRequiredService<IOptions<RecipeImportOptions>>().Value)
            ? ActivatorUtilities.CreateInstance<StandInRecipePhotoReader>(services)
            : ActivatorUtilities.CreateInstance<AnthropicRecipePhotoReader>(services);

    private static bool UsesStandInReader(IHostEnvironment environment, RecipeImportOptions options) =>
        options.StandInReader && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
}
