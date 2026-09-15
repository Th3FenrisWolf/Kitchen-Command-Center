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
        services.AddScoped<Admin.MemberNameLookup>();
        services.AddScoped<Admin.ContentItemNameLookup>();
        services.AddSingleton<Admin.Overview.IRecipeRollupSource, Admin.Overview.SqlRecipeRollupSource>();
        services.AddScoped<Admin.Overview.ContributionsOverviewService>();
        services.AddScoped<Admin.WebPageTabs.IContributionPageLookup, Admin.WebPageTabs.ContributionPageLookup>();
        services.AddScoped<Admin.WebPageTabs.ContributionTabService>();
        return services;
    }
}
