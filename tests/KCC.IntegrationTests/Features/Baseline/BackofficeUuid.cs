using System.Text.RegularExpressions;

namespace KCC.IntegrationTests.Features.Baseline;

// Umbraco's services store any Guid, but the backoffice's UmbId.validate rejects a key that is not an RFC 9562 UUID
// of version 1–8, so an item keyed otherwise cannot be opened there.
public static class BackofficeUuid
{
    private static readonly Regex Pattern =
        new("^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");

    public static bool IsAccepted(Guid key) => Pattern.IsMatch(key.ToString());
}
