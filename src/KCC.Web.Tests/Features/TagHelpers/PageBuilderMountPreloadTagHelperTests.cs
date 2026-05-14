#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KCC.Web.Features.ResourceStringEditing;
using KCC.Web.Features.TagHelpers;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace KCC.Web.Tests.Features.TagHelpers;

public class PageBuilderMountPreloadTagHelperTests
{
    [Fact]
    public async Task ProcessAsync_NoHttpContext_SuppressesOutput()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(null),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit));
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderOff_SuppressesOutput()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Off));
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderEdit_EmitsModulepreloadLink()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit));
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Equal("link", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal("modulepreload", output.Attributes["rel"].Value);
        Assert.Equal("/Features/PageBuilderMount.ts", output.Attributes["vite-href"].Value);
        Assert.Equal("true", output.Attributes["asp-append-version"].Value);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderReadOnly_EmitsModulepreloadLink()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly));
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Equal("link", output.TagName);
    }

    private static (TagHelperContext, TagHelperOutput) BuildContextAndOutput()
    {
        var attrs = new TagHelperAttributeList();
        var context = new TagHelperContext(attrs, new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(
            "page-builder-mount-preload",
            attrs,
            (cache, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        return (context, output);
    }

    private sealed class StubHttpContextAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    private sealed class StubPageBuilderModeProvider(PageBuilderMode mode) : IPageBuilderModeProvider
    {
        public PageBuilderMode GetMode(HttpContext httpContext) => mode;
    }
}
