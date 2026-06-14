using System.Collections;
using System.Text.Json;
using KCC.ResourceStrings.Editing;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;

namespace KCC.UnitTests.Features.ResourceStringEditing;

public class ResourceStringEditorTagHelperTests
{
    private static readonly IViteManifest Manifest = new StubViteManifest();
    private static readonly IViteDevServerStatus DevServerOff = new StubViteDevServerStatus(false);

    [Test]
    public async Task Process_CannotEdit_SuppressesOutput()
    {
        var sut = CreateTagHelper(canEdit: false);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var html = GetOutputHtml(output);
        _ = await Assert.That(html.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Process_CanEdit_RendersContextScript()
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
        _ = await Assert.That(html).Contains("kcc-rs-editor-context");

        var json = ExtractJsonContext(html);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        _ = await Assert.That(root.GetProperty("currentLanguage").GetString()).IsEqualTo("es");
        _ = await Assert.That(root.GetProperty("isPreviewMode").GetBoolean()).IsTrue();

        var available = root.GetProperty("availableLanguages").EnumerateArray().ToList();
        _ = await Assert.That(available.Count).IsEqualTo(2);
        _ = await Assert.That(available[0].GetProperty("code").GetString()).IsEqualTo("en");
        _ = await Assert.That(available[0].GetProperty("name").GetString()).IsEqualTo("English");
        _ = await Assert.That(available[1].GetProperty("code").GetString()).IsEqualTo("es");
        _ = await Assert.That(available[1].GetProperty("name").GetString()).IsEqualTo("Spanish");
    }

    [Test]
    public async Task Process_CanEdit_IsPreviewModeFalse_SerializesFalse()
    {
        var sut = CreateTagHelper(canEdit: true, isPreview: false);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var json = ExtractJsonContext(GetOutputHtml(output));
        using var doc = JsonDocument.Parse(json);
        _ = await Assert.That(doc.RootElement.GetProperty("isPreviewMode").GetBoolean()).IsFalse();
    }

    [Test]
    public async Task Process_CanEdit_ScriptUrlResolvesFromManifest()
    {
        var sut = CreateTagHelper(canEdit: true);
        var (context, output) = CreateTagHelperArgs();

        sut.Process(context, output);

        var html = GetOutputHtml(output);
        _ = await Assert.That(html).Contains("src=\"/assets/resourceStringEditor-abc123.js\"");
    }

    private static ResourceStringEditorTagHelper CreateTagHelper(
        bool canEdit,
        bool isPreview = false,
        string currentLanguage = "en",
        ContentLanguageOption[] languages = null) =>
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
        public string Src => null;
        public bool? IsEntry => true;
        public bool? IsDynamicEntry => false;
        public IEnumerable<string> Css => null;
        public IEnumerable<string> DynamicImports => null;
        public IEnumerable<string> Imports => null;
        public IEnumerable<string> Assets => null;
    }

    private sealed class StubViteManifest : IViteManifest
    {
        public IViteChunk this[string key] => new StubViteChunk("assets/resourceStringEditor-abc123.js");
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
