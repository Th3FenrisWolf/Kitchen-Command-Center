using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;

namespace KCC.Contributions.Admin;

public sealed record MemberDisplay(string Name, string Email);

/// <summary>Resolves member GUIDs to display values for the admin UI, with a "(deleted)" fallback.</summary>
public class MemberNameLookup(IInfoProvider<MemberInfo> memberInfoProvider, IProgressiveCache cache)
{
    internal const string DeletedFallback = "(deleted)";
    private const int CacheMinutes = 60;

    public IReadOnlyDictionary<Guid, MemberDisplay> Displays() =>
        cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"{MemberInfo.OBJECT_TYPE}|all");
                return memberInfoProvider.Get()
                    .ToArray()
                    .ToDictionary(
                        m => m.MemberGuid,
                        m => new MemberDisplay(
                            Format(
                                m.GetValue("MemberFirstName", string.Empty),
                                m.GetValue("MemberLastName", string.Empty),
                                m.MemberName),
                            m.MemberEmail ?? string.Empty));
            },
            new(CacheMinutes, nameof(MemberNameLookup), nameof(Displays)));

    /// <summary>Full name, else username; never null.</summary>
    internal static string Format(string firstName, string lastName, string userName)
    {
        var full = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
        if (full.Length > 0)
        {
            return full;
        }

        var fallback = userName?.Trim();
        return string.IsNullOrEmpty(fallback) ? DeletedFallback : fallback;
    }

    public static string DisplayOrDeleted(IReadOnlyDictionary<Guid, MemberDisplay> map, Guid memberGuid) =>
        map.TryGetValue(memberGuid, out var display) && !string.IsNullOrEmpty(display.Name)
            ? display.Name
            : DeletedFallback;

    public static string DisplayWithEmail(IReadOnlyDictionary<Guid, MemberDisplay> map, Guid memberGuid) =>
        !map.TryGetValue(memberGuid, out var display) || string.IsNullOrEmpty(display.Name)
            ? DeletedFallback
            : string.IsNullOrEmpty(display.Email)
                ? display.Name
                : $"{display.Name} ({display.Email})";
}
