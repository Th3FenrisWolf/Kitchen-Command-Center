using KitchenCommandCenter.Web.Features.Widgets.SideBySide;

namespace KitchenCommandCenter;

/// <summary>
/// Extended functionality of the auto-generated <see cref="PageBuilderPage"/>
/// </summary>
public partial class ImageItem
{
    public SideBySideItem MapToSideBySideItem()
    {
        return new SideBySideItem
        {
            Heading = "Image items don't have a title field, so this is a placeholder",
            Body = ImageAltText,
            Image = this,
        };
    }
}
