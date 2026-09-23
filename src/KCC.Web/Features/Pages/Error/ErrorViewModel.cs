using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Error;

public class ErrorViewModel : BasePageViewModel
{
    public int StatusCode { get; set; }
    public string Heading { get; set; }
    public string Body { get; set; }
}
