using KCC.ResourceStrings.Editing;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.ResourceStrings;

public static class ResourceStringServiceExtensions
{
    public static IServiceCollection AddKccResourceStrings(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IPageBuilderModeProvider, PageBuilderModeProvider>();
        services.AddSingleton<IPreviewModeProvider, PreviewModeProvider>();
        services.AddSingleton<IResourceStringEditorAccess, ResourceStringEditorAccess>();
        services.AddScoped<IResourceStringWriteRepository, ResourceStringWriteRepository>();
        services.AddScoped<IContentLanguageRepository, ContentLanguageRepository>();
        services.AddScoped<ResourceStringUpsertHandler>(sp => new(
            sp.GetRequiredService<IResourceStringWriteRepository>(),
            sp.GetRequiredService<IContentLanguageRepository>(),
            defaultLanguage: DefaultLanguageRetriever.GetName()));
        return services;
    }
}
