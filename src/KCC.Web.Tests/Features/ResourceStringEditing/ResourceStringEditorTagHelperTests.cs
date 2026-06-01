#nullable enable

using System.Collections;
using System.Text.Json;
using KCC.ResourceStrings.Editing;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringEditorTagHelperTests
{
    private static readonly IViteManifest Manifest = new StubViteManifest();
    private static readonly IViteDevServerStatus DevServerOff = new StubViteDevServerStatus(false);

    [Fact]
    public void Process_CannotEdit_SuppressesOutput()
    {
        var sut = CreateTagHelper(canEdit: false);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var html = GetOutputHtml(output);
        Assert.Empty(html);
    }

    [Fact]
    public void Process_CanEdit_RendersContextScript()
    {
        var languages = new[]
        {
            new ContentLanguageOption("en", "English"),
            new ContentLanguageOption("es", "Spanish"),
        };
        var sut = CreateTagHelper(canEdit: true, isPreview: true, currentLanguage: "es", languages: languages);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var html = GetOutputHtml(output);
        Assert.Contains("kcc-rs-editor-context", html);

        var json = ExtractJsonContext(html);
        using var doc = JsonDocument.Parse(json);
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
    public void Process_CanEdit_IsPreviewModeFalse_SerializesFalse()
    {
        var sut = CreateTagHelper(canEdit: true, isPreview: false);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var json = ExtractJsonContext(GetOutputHtml(output));
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isPreviewMode").GetBoolean());
    }

    [Fact]
    public void Process_CanEdit_ScriptUrlResolvesFromManifest()
    {
        var sut = CreateTagHelper(canEdit: true);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var html = GetOutputHtml(output);
        Assert.Contains("src=\"/assets/resourceStringEditor-abc123.js\"", html);
    }

    private static ResourceStringEditorTagHelper CreateTagHelper(
        bool canEdit,
        bool isPreview = false,
        string currentLanguage = "en",
        ContentLanguageOption[]? languages = null) =>
        new(
            new StubEditorAccess(canEdit, isPreview),
            new StubPreferredLanguageRetriever(currentLanguage),
            new StubContentLanguageRepository(languages ?? []),
            Manifest,
            DevServerOff);

    private static (TagHelperContext context, TagHelperOutput output) CreateTagHelperArgs()
    {
        var context = new TagHelperContext(
            "resource-string-editor",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "test-unique-id");

        var output = new TagHelperOutput(
            "resource-string-editor",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (context, output);
    }

    private static string GetOutputHtml(TagHelperOutput output)
    {
        using var writer = new StringWriter();
        output.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    private static string ExtractJsonContext(string html)
    {
        const string startMarker = "type=\"application/json\">";
        const string endMarker = "</script>";
        var startIndex = html.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
        var endIndex = html.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        return html[startIndex..endIndex];
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
