using CMS.DataEngine;
using CMS.Membership;

namespace KCC.Web.Features.Providers;

public class AuthorNameProvider(IInfoProvider<MemberInfo> memberInfoProvider)
{
    public async Task<string> Resolve(Guid authorMemberGuid, CancellationToken cancellationToken = default)
    {
        var names = await ResolveMany([authorMemberGuid], cancellationToken);
        return names.GetValueOrDefault(authorMemberGuid);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> authorMemberGuids, CancellationToken cancellationToken = default)
    {
        var guids = authorMemberGuids.Where(guid => guid != Guid.Empty).Distinct();

        if (!guids.Any())
        {
            return new Dictionary<Guid, string>();
        }

        var members = await memberInfoProvider.Get()
            .WhereIn(nameof(MemberInfo.MemberGuid), guids)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return members
            .Select(member => new
            {
                member.MemberGuid,
                DisplayName = FormatDisplayName(
                    member.GetValue("MemberFirstName", string.Empty),
                    member.GetValue("MemberLastName", string.Empty),
                    member.MemberName),
            })
            .Where(member => member.DisplayName is not null)
            .ToDictionary(member => member.MemberGuid, member => member.DisplayName);
    }

    public static string FormatDisplayName(string firstName, string lastName, string userName)
    {
        var fullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();

        if (fullName.Length > 0)
        {
            return fullName;
        }

        var fallback = userName?.Trim();
        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }
}
