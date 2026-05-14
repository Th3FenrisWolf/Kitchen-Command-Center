using System.Text.Json;
using System.Threading.Tasks;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.ResourceStringEditing;

public sealed class ResourceStringEditorViewComponent(
    IResourceStringEditorAccess editorAccess,
    IPreferredLanguageRetriever preferredLanguage,
    IContentLanguageRepository languageRepository)
    : ViewComponent
{
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

        var model = new ResourceStringEditorViewModel(JsonSerializer.Serialize(editorContext));

        return Task.FromResult<IViewComponentResult>(
            View("~/Features/ResourceStringEditing/ResourceStringEditor.cshtml", model));
    }
}
