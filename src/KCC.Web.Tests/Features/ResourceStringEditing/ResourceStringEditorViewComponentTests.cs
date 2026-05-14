#nullable enable

using System.Collections;
using System.Text.Json;
using KCC.ResourceStrings.Editing;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Vite.AspNetCore;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringEditorViewComponentTests
{
    private static readonly IViteManifest Manifest = new StubViteManifest();
    private static readonly IViteDevServerStatus DevServerOff = new StubViteDevServerStatus(false);

    [Fact]
    public async Task InvokeAsync_CannotEdit_ReturnsEmptyContent()
    {
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: false, isPreview: false),
            new StubPreferredLanguageRetriever("en"),
            new StubContentLanguageRepository(),
            Manifest,
            DevServerOff);

        var result = await sut.InvokeAsync();

        var content = Assert.IsType<ContentViewComponentResult>(result);
        Assert.Equal(string.Empty, content.Content);
    }

    [Fact]
    public async Task InvokeAsync_CanEdit_ReturnsViewResultWithSerializedContext()
    {
        var languages = new[]
        {
            new ContentLanguageOption("en", "English"),
            new ContentLanguageOption("es", "Spanish"),
        };
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: true, isPreview: true),
            new StubPreferredLanguageRetriever("es"),
            new StubContentLanguageRepository(languages),
            Manifest,
            DevServerOff);

        var result = await sut.InvokeAsync();

        var view = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<ResourceStringEditorViewModel>(view.ViewData!.Model);

        using var doc = JsonDocument.Parse(model.SerializedContext);
        var root = doc.RootElement;
        Assert.Equal("es", root.GetProperty("currentLanguage").GetString());
        Assert.True(root.GetProperty("isPreviewMode").GetBoolean());

        var available = root.GetProperty("availableLanguages").EnumerateArray().ToList();
        Assert.Equal(2, available.Count);
        Assert.Equal("en", available[0].GetProperty("code").GetString());
        Assert.Equal("English", available[0].GetProperty("name").GetString());
        Assert.Equal("es", available[1].GetProperty("code").GetString());
        Assert.Equal("Spanish", available[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task InvokeAsync_CanEdit_IsPreviewModeFalse_SerializesFalse()
    {
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: true, isPreview: false),
            new StubPreferredLanguageRetriever("en"),
            new StubContentLanguageRepository(),
            Manifest,
            DevServerOff);

        var result = await sut.InvokeAsync();

        var view = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<ResourceStringEditorViewModel>(view.ViewData!.Model);

        using var doc = JsonDocument.Parse(model.SerializedContext);
        Assert.False(doc.RootElement.GetProperty("isPreviewMode").GetBoolean());
    }

    [Fact]
    public async Task InvokeAsync_CanEdit_ScriptUrlResolvesFromManifest()
    {
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: true, isPreview: false),
            new StubPreferredLanguageRetriever("en"),
            new StubContentLanguageRepository(),
            Manifest,
            DevServerOff);

        var result = await sut.InvokeAsync();

        var view = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<ResourceStringEditorViewModel>(view.ViewData!.Model);
        Assert.Equal("/assets/resourceStringEditor-abc123.js", model.ScriptUrl);
    }

    private sealed class StubEditorAccess(bool canEdit, bool isPreview) : IResourceStringEditorAccess
    {
        public bool CanEdit() => canEdit;

        public bool IsPreviewMode() => isPreview;
    }

    private sealed class StubPreferredLanguageRetriever(string language) : IPreferredLanguageRetriever
    {
        public string Get() => language;
    }

    private sealed class StubContentLanguageRepository(params ContentLanguageOption[] languages)
        : IContentLanguageRepository
    {
        public bool Exists(string code) => languages.Any(l => l.Code == code);

        public IReadOnlyList<ContentLanguageOption> ListAll() => languages;
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

    private sealed class StubViteManifest : IViteManifest
    {
        public IViteChunk? this[string key] => new StubViteChunk("assets/resourceStringEditor-abc123.js");
        public IEnumerable<string> Keys => ["../KCC.ResourceStrings/Editing/ResourceStringEditor.ts"];
        public bool ContainsKey(string key) => true;
        public IEnumerator<IViteChunk> GetEnumerator() => ((IEnumerable<IViteChunk>)[this[""]!]).GetEnumerator();
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
