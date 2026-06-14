using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account.Settings;

public class AccountSettingsViewModel : BasePageViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string BackUrl { get; set; }
}
