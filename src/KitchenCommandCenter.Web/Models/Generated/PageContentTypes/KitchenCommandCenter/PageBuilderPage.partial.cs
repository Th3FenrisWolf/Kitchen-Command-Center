using System.Linq;
using KitchenCommandCenter.Web.Features.Widgets.SideBySide;

namespace KitchenCommandCenter;

/// <summary>
/// Extended functionality of the auto-generated <see cref="PageBuilderPage"/>
/// </summary>
public partial class PageBuilderPage
{
    public SideBySideItem MapToSideBySideItem()
    {
        return new SideBySideItem
        {
            Heading = MetadataTitle,
            Body = MetadataDescription,
            Image = MetadataImage.FirstOrDefault(),
            Button = new CtaButton
            {
                ButtonUrl = SystemFields.WebPageUrlPath,
                ButtonText = "Go to Page",
            },
        };
    }
}
