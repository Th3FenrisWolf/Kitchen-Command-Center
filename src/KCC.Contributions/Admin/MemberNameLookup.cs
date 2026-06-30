using CMS.DataEngine;
using CMS.Membership;

namespace KCC.Contributions.Admin;

/// <summary>Resolves member GUIDs to display names for admin listings, with a "(deleted)" fallback.</summary>
public class MemberNameLookup(IInfoProvider<MemberInfo> memberInfoProvider)
{
    internal const string DeletedFallback = "(deleted)";

    /// <summary>Builds a GUID-to-display-name map for the given member GUIDs.</summary>
    public IReadOnlyDictionary<Guid, string> Resolve(IEnumerable<Guid> memberGuids)
    {
        var guids = memberGuids.Where(g => g != Guid.Empty).Distinct().ToArray();
        if (guids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return memberInfoProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberGuid), guids)
            .ToArray()
            .ToDictionary(
                m => m.MemberGuid,
                m => Format(m.GetValue("MemberFirstName", string.Empty), m.GetValue("MemberLastName", string.Empty), m.MemberName));
    }

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

    /// <summary>Returns the resolved name or the "(deleted)" fallback.</summary>
    public static string DisplayOrDeleted(IReadOnlyDictionary<Guid, string> map, Guid memberGuid) =>
        map.TryGetValue(memberGuid, out var name) && !string.IsNullOrEmpty(name) ? name : DeletedFallback;
}
