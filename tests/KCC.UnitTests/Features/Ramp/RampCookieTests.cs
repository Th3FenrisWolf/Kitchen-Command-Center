using System.Security.Claims;
using KCC.Web.Features.Ramp;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace KCC.UnitTests.Features.Ramp;

public class RampCookieTests
{
    [Test]
    [Arguments("Light", "light")]
    [Arguments("Dark", "dark")]
    public async Task Write_MirrorsLightOrDark_InAYearLongHttpOnlyCookie(string ramp, string value)
    {
        var context = new DefaultHttpContext();

        RampCookie.Write(context, ramp);

        var cookie = SetCookie(context);
        _ = await Assert.That(cookie.Name.ToString()).IsEqualTo("kcc-ramp");
        _ = await Assert.That(cookie.Value.ToString()).IsEqualTo(value);
        _ = await Assert.That(cookie.HttpOnly).IsTrue();
        _ = await Assert.That(cookie.SameSite).IsEqualTo(Microsoft.Net.Http.Headers.SameSiteMode.Lax);
        _ = await Assert.That(cookie.Path.ToString()).IsEqualTo("/");
        _ = await Assert.That(cookie.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
        _ = await Assert.That(cookie.Secure).IsFalse();
    }

    [Test]
    public async Task Write_OverHttps_MarksTheCookieSecure()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        RampCookie.Write(context, Ramps.Dark);

        _ = await Assert.That(SetCookie(context).Secure).IsTrue();
    }

    [Test]
    public async Task Write_Device_DeletesTheCookie()
    {
        var context = new DefaultHttpContext();

        RampCookie.Write(context, Ramps.Device);

        var cookie = SetCookie(context);
        _ = await Assert.That(cookie.Name.ToString()).IsEqualTo("kcc-ramp");
        _ = await Assert.That(cookie.Expires < DateTimeOffset.UtcNow).IsTrue();
    }

    [Test]
    public async Task Clear_DeletesTheCookie()
    {
        var context = new DefaultHttpContext();

        RampCookie.Clear(context);

        _ = await Assert.That(SetCookie(context).Expires < DateTimeOffset.UtcNow).IsTrue();
    }

    [Test]
    [Arguments("kcc-ramp=dark", "dark")]
    [Arguments("kcc-ramp=light", "light")]
    [Arguments("kcc-ramp=sepia", null)]
    [Arguments("kcc-ramp=Dark", null)]
    [Arguments("", null)]
    public async Task Read_ForAMember_TakesLightOrDark(string cookies, string expected)
    {
        _ = await Assert.That(RampCookie.Read(Requesting(cookies, signedIn: true))).IsEqualTo(expected);
    }

    [Test]
    public async Task Read_ForAVisitor_IgnoresTheCookie()
    {
        _ = await Assert.That(RampCookie.Read(Requesting("kcc-ramp=dark", signedIn: false))).IsNull();
    }

    [Test]
    public async Task Read_AfterAWrite_TakesTheValueJustWritten()
    {
        var context = Requesting("kcc-ramp=light", signedIn: true);

        RampCookie.Write(context, Ramps.Dark);
        var afterDark = RampCookie.Read(context);
        RampCookie.Write(context, Ramps.Device);

        _ = await Assert.That(afterDark).IsEqualTo("dark");
        _ = await Assert.That(RampCookie.Read(context)).IsNull();
    }

    private static DefaultHttpContext Requesting(string cookies, bool signedIn)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = cookies;
        if (signedIn)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity("Test"));
        }

        return context;
    }

    private static SetCookieHeaderValue SetCookie(HttpContext context) =>
        SetCookieHeaderValue.Parse(context.Response.Headers.SetCookie.ToString());
}
