using KCC.Web.Features.Widgets.Base;

namespace KCC.Web.Features.Widgets.ImageCarousel;

public class ImageCarouselWidgetViewModel : BaseWidgetViewModel
{
    public IEnumerable<ImageItem> Items { get; set; } = [];
    public bool CropImages { get; set; }
    public int DisplayCount { get; set; }
    public ScrollBehavior ScrollBehavior { get; set; }
}

public enum ScrollBehavior
{
    Manual,
    Continuous,
    Automatic,
}
