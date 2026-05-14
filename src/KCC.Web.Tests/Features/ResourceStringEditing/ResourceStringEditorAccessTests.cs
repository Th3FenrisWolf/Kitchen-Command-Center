using KCC.Web.Features.ResourceStringEditing;
using Kentico.PageBuilder.Web.Mvc;
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
            new StubPageBuilderModeProvider(PageBuilderMode.Edit));

        Assert.False(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PageBuilderModeNotEdit_ReturnsFalse()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly));

        Assert.False(sut.CanEdit());
    }

    [Fact]
    public void CanEdit_PageBuilderModeEdit_ReturnsTrue()
    {
        var ctx = new DefaultHttpContext();

        var sut = new ResourceStringEditorAccess(
            new StubHttpContextAccessor(ctx),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit));

        Assert.True(sut.CanEdit());
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public StubHttpContextAccessor(HttpContext context)
        {
            HttpContext = context;
        }

        public HttpContext HttpContext { get; set; }
    }

    private sealed class StubPageBuilderModeProvider : IPageBuilderModeProvider
    {
        private readonly PageBuilderMode mode;

        public StubPageBuilderModeProvider(PageBuilderMode mode)
        {
            this.mode = mode;
        }

        public PageBuilderMode GetMode(HttpContext httpContext) => mode;
    }
}
