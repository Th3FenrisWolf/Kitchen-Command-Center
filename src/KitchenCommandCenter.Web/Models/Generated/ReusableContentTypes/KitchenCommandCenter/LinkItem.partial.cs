using System.Linq;
using CMS.Websites;
using KitchenCommandCenter.Web.Models.Common;

namespace KitchenCommandCenter;

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
                Type == "page"
                    ? Page?.FirstOrDefault()?.GetUrl().RelativePath
                    : Url,
        };
    }
}
