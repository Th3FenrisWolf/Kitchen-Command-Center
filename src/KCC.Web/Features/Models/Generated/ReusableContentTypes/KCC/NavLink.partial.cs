using CMS.Websites;
using KCC.Web.Features.Models.Common;

namespace KCC;

/// <summary>
/// Extended functionality of the auto-generated <see cref="NavLink"/>.
/// </summary>
public partial class NavLink
{
    public PageLink MapToPageLink() => new()
    {
        DisplayText = DisplayText,
        Target = Target,
        Url = Type.Equals("page", StringComparison.Ordinal)
            ? Page?.FirstOrDefault()?.GetUrl().RelativePath
            : Url,
    };
}
