using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Account;

public sealed record AccountUrls(string Account, string Login, string Settings, string RegistrationComplete);

public class AccountPageQueries(IPublishedContentQuery contentQuery)
{
    public AccountUrls GetUrls()
    {
        var account = contentQuery.ContentAtRoot().OfType<HomePage>().FirstOrDefault()?.Children<AccountPage>().FirstOrDefault();
        return new AccountUrls(
            account?.Url(),
            account?.Children<LoginPage>().FirstOrDefault()?.Url(),
            account?.Children<AccountSettingsPage>().FirstOrDefault()?.Url(),
            account?.Children<RegistrationCompletePage>().FirstOrDefault()?.Url());
    }
}
