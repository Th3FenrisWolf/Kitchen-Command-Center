namespace KCC.Web.Features.Members;

/// <summary>
/// Resolves member display names for author attribution, live from the member record
/// so renames in Account Settings propagate everywhere.
/// </summary>
public interface IAuthorNameResolver
{
    /// <summary>
    /// Resolves the display name for a single member.
    /// </summary>
    /// <param name="authorMemberGuid">The member's guid, as stamped on authored content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The display name, or null when the guid is empty or the member no longer exists — callers hide the attribution line.</returns>
    Task<string> Resolve(Guid authorMemberGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves display names for a set of members in one query.
    /// </summary>
    /// <param name="authorMemberGuids">Member guids; empty guids and duplicates are ignored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Map of member guid to display name; empty or unknown guids are absent.</returns>
    Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> authorMemberGuids, CancellationToken cancellationToken = default);
}
