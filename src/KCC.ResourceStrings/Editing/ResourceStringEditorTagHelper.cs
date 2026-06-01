using System.Text.Encodings.Web;
using System.Text.Json;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;

namespace KCC.ResourceStrings.Editing;

[HtmlTargetElement("resource-string-editor", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ResourceStringEditorTagHelper(
    IResourceStringEditorAccess editorAccess,
    IPreferredLanguageRetriever preferredLanguage,
    IContentLanguageRepository languageRepository,
    IViteManifest manifest,
    IViteDevServerStatus devServer)
    : TagHelper
{
    private const string EntryKey = "../KCC.ResourceStrings/Client/src/resource-strings/ResourceStringEditor.ts";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default,
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!editorAccess.CanEdit())
        {
            output.SuppressOutput();
            return;
        }

        var availableLanguages = languageRepository.ListAll()
            .Select(l => new { code = l.Code, name = l.Name });

        var editorContext = new
        {
            currentLanguage = preferredLanguage.Get(),
            availableLanguages,
            isPreviewMode = editorAccess.IsPreviewMode(),
        };

        var serializedContext = JsonSerializer.Serialize(editorContext, s_jsonOptions);
        var scriptUrl = ResolveScriptUrl();

        output.TagName = null;
        output.Content.SetHtmlContent(
            $"""
            <script id="kcc-rs-editor-context" type="application/json">{serializedContext}</script>
            <script type="module" src="{scriptUrl}"></script>
            """);
    }

    private string ResolveScriptUrl()
    {
        if (devServer.IsEnabled)
        {
            var serverUrl = devServer.ServerUrl.TrimEnd('/');
            var webProjectDir = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var absolutePath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(webProjectDir, EntryKey)).Replace('\\', '/');
            return $"{serverUrl}/@fs/{absolutePath}";
        }

        var chunk = manifest[EntryKey]
            ?? throw new InvalidOperationException(
                $"Vite manifest is missing entry '{EntryKey}'.");
        return $"/{chunk.File}";
    }
}
