using System.Text.Encodings.Web;
using System.Text.Json;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Vite.AspNetCore;

namespace KCC.ResourceStrings.Editing;

public sealed record ResourceStringEditorViewModel(string SerializedContext, string ScriptUrl);

public sealed class ResourceStringEditorViewComponent(
    IResourceStringEditorAccess editorAccess,
    IPreferredLanguageRetriever preferredLanguage,
    IContentLanguageRepository languageRepository,
    IViteManifest manifest,
    IViteDevServerStatus devServer)
    : ViewComponent
{
    private const string EntryKey = "../KCC.ResourceStrings/Client/src/resource-strings/ResourceStringEditor.ts";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default,
    };

    public Task<IViewComponentResult> InvokeAsync()
    {
        if (!editorAccess.CanEdit())
        {
            return Task.FromResult<IViewComponentResult>(Content(string.Empty));
        }

        var availableLanguages = languageRepository.ListAll()
            .Select(l => new { code = l.Code, name = l.Name });

        var editorContext = new
        {
            currentLanguage = preferredLanguage.Get(),
            availableLanguages,
            isPreviewMode = editorAccess.IsPreviewMode(),
        };

        var model = new ResourceStringEditorViewModel(
            JsonSerializer.Serialize(editorContext, s_jsonOptions),
            ResolveScriptUrl());

        return Task.FromResult<IViewComponentResult>(
            View("/Editing/ResourceStringEditor.cshtml", model));
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
