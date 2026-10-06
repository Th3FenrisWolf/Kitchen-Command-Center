using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Account;

public static class SignInRedirect
{
    public static IActionResult To(string loginUrl, HttpRequest request) =>
        loginUrl is null
            ? new NotFoundResult()
            : new RedirectResult(UrlFor(loginUrl, request.Path + request.QueryString));

    public static string UrlFor(string loginUrl, string returnUrl) => $"{loginUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}";
}
