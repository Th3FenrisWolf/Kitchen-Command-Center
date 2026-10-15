using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

public static class TestImportSpend
{
    public static void Set(IServiceProvider services, string month, long totalMicroUsd)
    {
        using var scope = services.GetRequiredService<ICoreScopeProvider>().CreateCoreScope();
        scope.WriteLock(Constants.Locks.KeyValues);
        services.GetRequiredService<IKeyValueService>().SetValue(Key(month), totalMicroUsd.ToString(CultureInfo.InvariantCulture));
        scope.Complete();
    }

    public static long? Read(IServiceProvider services, string month)
    {
        using var scope = services.GetRequiredService<ICoreScopeProvider>().CreateCoreScope(autoComplete: true);
        scope.ReadLock(Constants.Locks.KeyValues);
        var total = services.GetRequiredService<IKeyValueService>().GetValue(Key(month));
        return total is null ? null : long.Parse(total, CultureInfo.InvariantCulture);
    }

    private static string Key(string month) => $"KCC.RecipeImport.SpendMicroUsd.{month}";
}
