#nullable enable

using System.Collections;
using KCC.ResourceStrings.Editing;
using KCC.Web.Features.TagHelpers;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;
using Xunit;

namespace KCC.Web.Tests.Features.TagHelpers;

public class PageBuilderMountPreloadTagHelperTests
{
    private static readonly IViteManifest Manifest = new StubViteManifest("assets/pageBuilderMount-abc123.js");
    private static readonly IViteDevServerStatus DevServerOff = new StubViteDevServerStatus(false);

    [Fact]
    public async Task ProcessAsync_NoHttpContext_SuppressesOutput()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(null),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderOff_SuppressesOutput()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderEdit_EmitsModulepreloadLink()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Equal("link", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal("modulepreload", output.Attributes["rel"].Value);
        Assert.Equal("/assets/pageBuilderMount-abc123.js", output.Attributes["href"].Value);
    }

    [Fact]
    public async Task ProcessAsync_PageBuilderReadOnly_EmitsModulepreloadLink()
    {
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Equal("link", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_DevServerEnabled_EmitsDevServerHref()
    {
        var devServerOn = new StubViteDevServerStatus(true, "http://localhost:5173");
        var sut = new PageBuilderMountPreloadTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            devServerOn);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        Assert.Equal("http://localhost:5173/Features/PageBuilderMount.ts", output.Attributes["href"].Value);
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

    private sealed class StubViteChunk(string file) : IViteChunk
    {
        public string File => file;
        public string? Src => null;
        public bool? IsEntry => true;
        public bool? IsDynamicEntry => false;
        public IEnumerable<string>? Css => null;
        public IEnumerable<string>? DynamicImports => null;
        public IEnumerable<string>? Imports => null;
        public IEnumerable<string>? Assets => null;
    }

    private sealed class StubViteManifest(string file) : IViteManifest
    {
        public IViteChunk? this[string key] => new StubViteChunk(file);
        public IEnumerable<string> Keys => [key];
        private readonly string key = "Features/PageBuilderMount.ts";
        public bool ContainsKey(string key) => key == this.key;
        public IEnumerator<IViteChunk> GetEnumerator() => ((IEnumerable<IViteChunk>)[this[key]!]).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StubViteDevServerStatus(bool isEnabled, string basePath = "") : IViteDevServerStatus
    {
        public bool IsEnabled => isEnabled;
        public bool IsMiddlewareEnable => false;
        public string BasePath => basePath;
        public string ServerUrl => basePath;
        public string ServerUrlWithBasePath => basePath;
    }
}
