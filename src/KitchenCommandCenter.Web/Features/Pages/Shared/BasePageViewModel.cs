namespace KitchenCommandCenter.Web.Features.Pages.Shared;

public class BasePageViewModel
{
    public int WebPageItemID { get; set; }
    public bool ShowBreadcrumbs { get; set; }

    /* Metadata/OpenGraph */
    public string Title { get; set; }
    public string Description { get; set; }
    public string Keywords { get; set; }
    public string PublishDate { get; set; }
    public string ImagePath { get; set; }
    public int ImageHeight { get; set; }
    public int ImageWidth { get; set; }
    public string TwitterCard { get; set; }
    public string TwitterSite { get; set; }
    public string TwitterCreator { get; set; }
    public string TwitterImagePath { get; set; }
}
