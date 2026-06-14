using System.Collections;
using KCC.ResourceStrings.Editing;
using KCC.Web.Features.TagHelpers;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;

namespace KCC.UnitTests.Features.TagHelpers;

public class PageBuilderMountScriptTagHelperTests
{
    private static readonly IViteManifest Manifest = new StubViteManifest("assets/pageBuilderMount-abc123.js");
    private static readonly IViteDevServerStatus DevServerOff = new StubViteDevServerStatus(false);

    [Test]
    public async Task ProcessAsync_NoHttpContext_SuppressesOutput()
    {
        var sut = new PageBuilderMountScriptTagHelper(
            new StubHttpContextAccessor(null),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        _ = await Assert.That(output.TagName).IsNull();
    }

    [Test]
    public async Task ProcessAsync_PageBuilderOff_SuppressesOutput()
    {
        var sut = new PageBuilderMountScriptTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Off),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        _ = await Assert.That(output.TagName).IsNull();
    }

    [Test]
    public async Task ProcessAsync_PageBuilderEdit_EmitsModuleScript()
    {
        var sut = new PageBuilderMountScriptTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        _ = await Assert.That(output.TagName).IsEqualTo("script");
        _ = await Assert.That(output.TagMode).IsEqualTo(TagMode.StartTagAndEndTag);
        _ = await Assert.That(output.Attributes["type"].Value).IsEqualTo("module");
        _ = await Assert.That(output.Attributes["src"].Value).IsEqualTo("/assets/pageBuilderMount-abc123.js");
    }

    [Test]
    public async Task ProcessAsync_PageBuilderReadOnly_EmitsModuleScript()
    {
        var sut = new PageBuilderMountScriptTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.ReadOnly),
            Manifest,
            DevServerOff);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        _ = await Assert.That(output.TagName).IsEqualTo("script");
    }

    [Test]
    public async Task ProcessAsync_DevServerEnabled_EmitsDevServerSrc()
    {
        var devServerOn = new StubViteDevServerStatus(true, "http://localhost:5173");
        var sut = new PageBuilderMountScriptTagHelper(
            new StubHttpContextAccessor(new DefaultHttpContext()),
            new StubPageBuilderModeProvider(PageBuilderMode.Edit),
            Manifest,
            devServerOn);
        var (context, output) = BuildContextAndOutput();

        await sut.ProcessAsync(context, output);

        _ = await Assert.That(output.Attributes["src"].Value).IsEqualTo("http://localhost:5173/Features/PageBuilderMount.ts");
    }

    private static (TagHelperContext, TagHelperOutput) BuildContextAndOutput()
    {
        var attrs = new TagHelperAttributeList();
        var context = new TagHelperContext(attrs, new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(
            "page-builder-mount-script",
            attrs,
            (cache, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        return (context, output);
    }

    private sealed class StubHttpContextAccessor(HttpContext context) : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; } = context;
    }

    private sealed class StubPageBuilderModeProvider(PageBuilderMode mode) : IPageBuilderModeProvider
    {
        public PageBuilderMode GetMode(HttpContext httpContext) => mode;
    }

    private sealed class StubViteChunk(string file) : IViteChunk
    {
        public string File => file;
        public string Src => null;
        public bool? IsEntry => true;
        public bool? IsDynamicEntry => false;
        public IEnumerable<string> Css => null;
        public IEnumerable<string> DynamicImports => null;
        public IEnumerable<string> Imports => null;
        public IEnumerable<string> Assets => null;
    }

    private sealed class StubViteManifest(string file) : IViteManifest
    {
        public IViteChunk this[string key] => new StubViteChunk(file);
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
