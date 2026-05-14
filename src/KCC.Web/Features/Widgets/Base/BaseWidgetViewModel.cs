namespace KCC.Web.Features.Widgets.Base;

public class BaseWidgetViewModel
{
    public BaseWidgetProperties Properties { get; set; }
    public Dictionary<string, string> ResourceStrings { get; set; } = [];
}
