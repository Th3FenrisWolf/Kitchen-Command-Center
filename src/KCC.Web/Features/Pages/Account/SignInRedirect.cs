using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Account;

public static class SignInRedirect
{
    // Sends a signed-out visitor to the login page, which returns them here once they have signed in.
    public static IActionResult To(string loginUrl, HttpRequest request) =>
        loginUrl is null
            ? new NotFoundResult()
            : new RedirectResult($"{loginUrl}?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
}
