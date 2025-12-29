namespace KitchenCommandCenter.Web.Models.Common;

public class PageLink
{
    public string DisplayText { get; set; }
    public string Target { get; set; }
    public string Url { get; set; }
    public string Icon { get; set; } // Not currently being set, as we don't have a solution in place yet for icons
    public string ShortDescription { get; set; }
}
