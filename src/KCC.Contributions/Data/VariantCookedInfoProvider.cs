using CMS.Core;
using CMS.Helpers;

namespace KCC.Contributions.Data;

public partial class VariantCookedInfoProvider
{
    private const int CacheMinutes = 60;

    /// <summary>Per-variant cooked count.</summary>
    internal static IReadOnlyDictionary<Guid, int> CountByVariant(IEnumerable<Guid> variantGuids) =>
        variantGuids.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Total number of cooked rows.</summary>
    internal static int CountRows(IReadOnlyCollection<Guid> variantGuids) => variantGuids.Count;

    private static string[] CacheKeys => [$"{VariantCookedInfo.OBJECT_TYPE}|all"];

    public int GetCookedCountForVariant(Guid variantGuid)
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                return Get().WhereEquals(nameof(VariantCookedInfo.VariantGuid), variantGuid).Count;
            },
            new CacheSettings(CacheMinutes, VariantCookedInfo.OBJECT_TYPE, "count-variant", variantGuid));
    }

    public IReadOnlyDictionary<Guid, int> GetCookedCountsForVariants(IReadOnlyCollection<Guid> variantGuids)
    {
        if (variantGuids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                var rows = Get().WhereIn(nameof(VariantCookedInfo.VariantGuid), variantGuids.ToArray()).ToArray();
                return CountByVariant(rows.Select(r => r.VariantGuid));
            },
            new CacheSettings(CacheMinutes, VariantCookedInfo.OBJECT_TYPE, "count-variants", string.Join("|", variantGuids)));
    }

    public int GetRecipeCookedCount(Guid recipeGuid)
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                return Get().WhereEquals(nameof(VariantCookedInfo.RecipeGuid), recipeGuid).Count;
            },
            new CacheSettings(CacheMinutes, VariantCookedInfo.OBJECT_TYPE, "count-recipe", recipeGuid));
    }

    public bool HasMemberCooked(Guid variantGuid, Guid memberGuid) =>
        Get()
            .WhereEquals(nameof(VariantCookedInfo.VariantGuid), variantGuid)
            .WhereEquals(nameof(VariantCookedInfo.MemberGuid), memberGuid)
            .TopN(1)
            .Count > 0;

    public void MarkCooked(Guid variantGuid, Guid recipeGuid, Guid memberGuid)
    {
        if (HasMemberCooked(variantGuid, memberGuid))
        {
            return;
        }

        Set(new VariantCookedInfo
        {
            VariantGuid = variantGuid,
            RecipeGuid = recipeGuid,
            MemberGuid = memberGuid,
            CookedCreated = DateTime.UtcNow,
        });
    }

    public bool UnmarkCooked(Guid variantGuid, Guid memberGuid)
    {
        var existing = Get()
            .WhereEquals(nameof(VariantCookedInfo.VariantGuid), variantGuid)
            .WhereEquals(nameof(VariantCookedInfo.MemberGuid), memberGuid)
            .TopN(1)
            .FirstOrDefault();

        if (existing is null)
        {
            return false;
        }

        Delete(existing);
        return true;
    }
}
