using CMS.DataEngine;
using CMS.Membership;

namespace KCC.Web.Features.Members;

/// <summary>
/// Default <see cref="IAuthorNameResolver"/> backed by the member info provider.
/// </summary>
public class AuthorNameResolver(IInfoProvider<MemberInfo> memberInfoProvider) : IAuthorNameResolver
{
    /// <inheritdoc />
    public async Task<string> Resolve(Guid authorMemberGuid, CancellationToken cancellationToken = default)
    {
        var names = await ResolveMany([authorMemberGuid], cancellationToken);
        return names.GetValueOrDefault(authorMemberGuid);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> authorMemberGuids, CancellationToken cancellationToken = default)
    {
        var guids = authorMemberGuids.Where(guid => guid != Guid.Empty).Distinct().ToArray();

        if (guids.Length is 0)
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

    /// <summary>
    /// Formats a member display name.
    /// </summary>
    /// <param name="firstName">Member first name; may be null or whitespace.</param>
    /// <param name="lastName">Member last name; may be null or whitespace.</param>
    /// <param name="userName">Member username, used as fallback.</param>
    /// <returns>Full name ("First Last") when any name part exists; otherwise the username; null when nothing usable.</returns>
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
