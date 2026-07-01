using KCC.Contributions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.Contributions;

public static class ContributionsServiceExtensions
{
    /// <summary>Registers the Community Contributions provider interfaces.</summary>
    public static IServiceCollection AddKccContributions(this IServiceCollection services)
    {
        services.AddScoped<IVariantReviewInfoProvider, VariantReviewInfoProvider>();
        services.AddScoped<IVariantCookNoteInfoProvider, VariantCookNoteInfoProvider>();
        services.AddScoped<IVariantCookedInfoProvider, VariantCookedInfoProvider>();
        services.AddScoped<KCC.Contributions.Admin.MemberNameLookup>();
        services.AddScoped<KCC.Contributions.Admin.VariantNameLookup>();
        return services;
    }
}
