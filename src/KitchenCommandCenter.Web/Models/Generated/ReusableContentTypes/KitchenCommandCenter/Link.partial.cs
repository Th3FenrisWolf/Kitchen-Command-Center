using System.Linq;
using KitchenCommandCenter.Web.Models.Common;

namespace KitchenCommandCenter;

/// <summary>
/// Extended functionality of the auto-generated <see cref="Link"/>.
/// </summary>
public partial class Link
{
    public PageLink MapToPageLink()
    {
        return new PageLink
        {
            DisplayText = LinkDisplayText,
            Target = LinkTarget,
            Url =
                LinkType == "page"
                    ? LinkPage?.FirstOrDefault()?.SystemFields.WebPageUrlPath
                    : LinkUrl,
            ShortDescription = LinkDescription,
        };
    }
}
