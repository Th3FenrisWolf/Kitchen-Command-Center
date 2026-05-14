#nullable enable

using System;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using KCC.Admin.Models.Retrievers;
using Microsoft.AspNetCore.Mvc;
using ResourceStrings;

namespace KCC.Web.Features.ResourceStringEditing;

// v1 trust model: these endpoints have no permission gate. The editor JS that
// calls them only ships when the layout's CanEdit() check returns true (i.e.
// the user is rendering a page in PageBuilderMode.Edit, which Kentico grants
// only to authenticated admins viewing through the admin preview iframe). The
// API URL is not advertised elsewhere. CSRF is mitigated the same way: a
// cross-site request would not have the editor bundle loaded and would not
// carry valid Kentico admin cookies (SameSite=Lax). Revisit if the mode-
// detection chain is broadened or these endpoints become discoverable.
[ApiController]
[Route("api/resource-strings")]
public sealed class ResourceStringEditingController(
    IResourceStringInfoProvider stringProvider,
    IInfoProvider<ResourceStringTranslationInfo> translationProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider,
    ResourceStringUpsertHandler upsertHandler)
    : ControllerBase
{
    [HttpGet]
    public ActionResult<ResourceStringValueResponse> Get(
        [FromQuery] string key,
        [FromQuery] string language)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(language))
        {
            return BadRequest(new { error = "key and language are required" });
        }

        // Resolve the fallback in the context of the language being edited so
        // its language-fallback chain (ContentLanguageFallbackContentLanguageID)
        // is honored. Using GetOrDefault(key) without a language uses the API
        // request's preferred language — usually the default — which would
        // bypass the editing language's chain.
        var fallbackValue = stringProvider.GetOrDefault(key, language);

        var stringRow = stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .FirstOrDefault();

        if (stringRow is null)
        {
            return Ok(new ResourceStringValueResponse(
                Key: key,
                Language: language,
                Value: null,
                FallbackValue: fallbackValue,
                Exists: false));
        }

        var defaultLanguage = DefaultLanguageRetriever.GetName();
        if (string.Equals(language, defaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ResourceStringValueResponse(
                Key: key,
                Language: language,
                Value: stringRow.ResourceStringValue,
                FallbackValue: fallbackValue,
                Exists: true));
        }

        var languageId = languageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), language)
            .Column(nameof(ContentLanguageInfo.ContentLanguageID))
            .TopN(1)
            .GetScalarResult(0);

        if (languageId is 0)
        {
            return BadRequest(new { error = $"Unknown language: {language}" });
        }

        var translation = translationProvider.Get()
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID), stringRow.ResourceStringID)
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationContentLanguageID), languageId)
            .TopN(1)
            .FirstOrDefault();

        return Ok(new ResourceStringValueResponse(
            Key: key,
            Language: language,
            Value: translation?.ResourceStringTranslationValue,
            FallbackValue: fallbackValue,
            Exists: translation is not null));
    }

    [HttpPut]
    public async Task<ActionResult<ResourceStringUpsertResponse>> Put(
        [FromBody] ResourceStringUpsertRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await upsertHandler.UpsertAsync(request);

            // Re-resolve through the provider so the response carries what the
            // page would actually render (chain-walked, empty -> key fallback)
            // rather than the raw saved value. Without this, clearing a value
            // would echo back "" and the in-place DOM update would leave the
            // marker with no visible text — making it uneditable.
            var resolvedValue = stringProvider.GetOrDefault(request.Key, request.Language);
            return Ok(new ResourceStringUpsertResponse(request.Key, request.Language, resolvedValue));
        }
        catch (InvalidLanguageException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
