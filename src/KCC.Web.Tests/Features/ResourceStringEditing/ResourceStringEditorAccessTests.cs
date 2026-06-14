using System.Security.Claims;
using KCC.ResourceStrings.Editing;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringEditorAccessTests
{
    [Fact]
    public void CanEdit_NoHttpContext_ReturnsFalse()
    {
        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(null),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            new StubPreviewModeProvider(false));

        Assert.False(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PageBuilderEdit_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            new StubPreviewModeProvider(false));

        Assert.True(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PreviewWithAdminAuthentication_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext
        {
            RequestServices = new StubServiceProvider(new StubAuthenticationService(authenticated: true)),
        };

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            new StubPreviewModeProvider(true));

        Assert.True(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PreviewWithoutAdminAuthentication_ReturnsFalse()
    {
        var ctx = new DefaultHttpContext
        {
            RequestServices = new StubServiceProvider(new StubAuthenticationService(authenticated: false)),
        };

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            new StubPreviewModeProvider(true));

        Assert.False(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PageBuilderReadOnly_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly),
            new StubPreviewModeProvider(false));

        Assert.True(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_NeitherEditNorPreview_ReturnsFalse()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            new StubPreviewModeProvider(false));

        Assert.False(sut.CanEdit());
    }

    [Fact]
    public void IsPreviewMode_PageBuilderReadOnly_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly),
            new StubPreviewModeProvider(false));

        Assert.True(sut.IsPreviewMode());
    }

    [Fact]
    public void IsPreviewMode_PreviewEnabled_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            new StubPreviewModeProvider(true));

        Assert.True(sut.IsPreviewMode());
    }

    [Fact]
    public void IsPreviewMode_PreviewDisabled_ReturnsFalse()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            new StubPreviewModeProvider(false));

        Assert.False(sut.IsPreviewMode());
    }

    [Fact]
    public void IsPreviewMode_PreviewEnabledButPageBuilderEdit_ReturnsFalse()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            new StubPreviewModeProvider(true));

        Assert.False(sut.IsPreviewMode());
    }

    [Fact]
    public void IsPreviewMode_NoHttpContext_ReturnsFalse()
    {
        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(null),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            new StubPreviewModeProvider(true));

        Assert.False(sut.IsPreviewMode());
    }

    private sealed class StubHttpContextAccessor(HttpContext context) : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; } = context;
    }

    private sealed class StubPageBuilderModeProvider(PageBuilderMode mode) : IPageBuilderModeProvider
    {
        public PageBuilderMode GetMode(HttpContext httpContext) => mode;
    }

    private sealed class StubPreviewModeProvider(bool isPreview) : IPreviewModeProvider
    {
        public bool IsPreview(HttpContext httpContext) => isPreview;
    }

    private sealed class StubServiceProvider(IAuthenticationService authenticationService) : IServiceProvider
    {
        public object GetService(Type serviceType) =>
            serviceType == typeof(IAuthenticationService) ? authenticationService : null;
    }

    private sealed class StubAuthenticationService(bool authenticated) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
            Task.FromResult(authenticated
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(scheme)), scheme))
                : AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) =>
            Task.CompletedTask;
    }
}
