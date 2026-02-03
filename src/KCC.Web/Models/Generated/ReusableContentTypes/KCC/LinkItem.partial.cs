using CMS.Websites;
using KCC.Web.Models.Common;

namespace KCC;

/// <summary>
/// Extended functionality of the auto-generated <see cref="LinkItem"/>.
/// </summary>
public partial class LinkItem
{
    public PageLink MapToPageLink()
    {
        return new PageLink
        {
            DisplayText = DisplayText,
            Target = Target,
            Url =
                Type.Equals("page", StringComparison.Ordinal)
                    ? Page?.FirstOrDefault()?.GetUrl().RelativePath
                    : Url,
        };
    }
}
