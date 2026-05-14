#nullable enable

using CMS.ContentEngine;
using CMS.DataEngine;
using KCC.ResourceStrings.Data;
using Kentico.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KCC.ResourceStrings.Editing;

[ApiController]
[Route("api/resource-strings")]
// Only Xperience admins (forms-auth or external SSO) may read or modify resource
// strings. Without this, shareable-preview recipients — or any anonymous client —
// could call /api/resource-strings directly and bypass the UI-level gate.
[Authorize(AuthenticationSchemes = $"{AdminIdentityConstants.APPLICATION_SCHEME},{AdminIdentityConstants.EXTERNAL_SCHEME}")]
public sealed class ResourceStringEditorController(
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
    public ActionResult<ResourceStringUpsertResponse> Put(
        [FromBody] ResourceStringUpsertRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            upsertHandler.Upsert(request);

            var resolvedValue = stringProvider.GetOrDefault(request.Key, request.Language);
            return Ok(new ResourceStringUpsertResponse(request.Key, request.Language, resolvedValue));
        }
        catch (InvalidLanguageException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
