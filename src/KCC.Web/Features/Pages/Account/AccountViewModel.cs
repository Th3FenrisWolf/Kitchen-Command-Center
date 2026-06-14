using System.Globalization;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account;

public class AccountViewModel : BasePageViewModel
{
    public string DisplayName { get; set; }
    public string Initials { get; set; }
    public string MemberSince { get; set; }

    public static string ComputeInitials(string firstName, string lastName, string fallback)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();

        var initials = string.Concat(
            first.Length > 0 ? first[..1] : string.Empty,
            last.Length > 0 ? last[..1] : string.Empty);

        if (initials.Length == 0)
        {
            var source = (fallback ?? string.Empty).Trim();
            initials = source.Length >= 2 ? source[..2] : source;
        }

        return initials.ToUpperInvariant();
    }

    public static string FormatMemberSince(DateTime? created) =>
        created.HasValue
            ? created.Value.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            : string.Empty;
}
