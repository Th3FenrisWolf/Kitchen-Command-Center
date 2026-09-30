using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Home;

public class HomeViewModel : BasePageViewModel
{
    public IReadOnlyList<HomeSection> Sections { get; set; } = [];
}
