// KCC.Web does not enable nullable globally; this file opts in so the
// repository contracts can express semantically meaningful nullability
// (string? translation values where null means "delete the translation").
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using ResourceStrings;

namespace KCC.Web.Features.ResourceStringEditing;

public interface IResourceStringWriteRepository
{
    Task UpsertStringAsync(string key, string value);

    Task<bool> StringExistsAsync(string key);

    Task UpsertTranslationAsync(string key, string language, string? value);
}

public sealed record ContentLanguageOption(string Code, string Name);

public interface IContentLanguageRepository
{
    Task<bool> ExistsAsync(string code);

    IReadOnlyList<ContentLanguageOption> ListAll();
}

public sealed class InvalidLanguageException(string language)
    : Exception($"Unknown content language code: {language}")
{
    public string Language { get; } = language;
}

public sealed class ResourceStringUpsertHandler(
    IResourceStringWriteRepository writeRepo,
    IContentLanguageRepository languageRepo,
    string defaultLanguage)
{
    public async Task<ResourceStringUpsertResponse> UpsertAsync(ResourceStringUpsertRequest request)
    {
        if (!await languageRepo.ExistsAsync(request.Language))
        {
            throw new InvalidLanguageException(request.Language);
        }

        var isDefault = string.Equals(request.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase);

        if (isDefault)
        {
            var value = request.Value ?? string.Empty;
            await writeRepo.UpsertStringAsync(request.Key, value);
            return new ResourceStringUpsertResponse(request.Key, request.Language, value);
        }

        // TOCTOU: two concurrent saves of a brand-new key here can either deadlock on
        // a unique constraint or produce duplicate parent rows. Last-write-wins is
        // acceptable per spec; if a real concurrent collision becomes a problem,
        // wrap these two writes in a CMSTransactionScope or catch duplicate-key.
        if (!await writeRepo.StringExistsAsync(request.Key))
        {
            await writeRepo.UpsertStringAsync(request.Key, string.Empty);
        }

        await writeRepo.UpsertTranslationAsync(request.Key, request.Language, request.Value);
        return new ResourceStringUpsertResponse(request.Key, request.Language, request.Value ?? string.Empty);
    }
}

internal sealed class ResourceStringWriteRepository(
    IInfoProvider<ResourceStringInfo> stringProvider,
    IInfoProvider<ResourceStringTranslationInfo> translationProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider)
    : IResourceStringWriteRepository
{
    public Task UpsertStringAsync(string key, string value)
    {
        var existing = stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .FirstOrDefault();

        if (existing is null)
        {
            existing = new ResourceStringInfo
            {
                ResourceStringKey = key,
                ResourceStringValue = value,
            };
        }
        else
        {
            existing.ResourceStringValue = value;
        }

        stringProvider.Set(existing);
        return Task.CompletedTask;
    }

    public Task<bool> StringExistsAsync(string key) =>
        Task.FromResult(stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .Any());

    public Task UpsertTranslationAsync(string key, string language, string? value)
    {
        var stringRow = stringProvider.Get()
            .WhereEquals(nameof(ResourceStringInfo.ResourceStringKey), key)
            .TopN(1)
            .FirstOrDefault();
        if (stringRow is null)
        {
            return Task.CompletedTask;
        }

        var languageId = languageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), language)
            .Column(nameof(ContentLanguageInfo.ContentLanguageID))
            .TopN(1)
            .GetScalarResult(0);
        if (languageId is 0)
        {
            return Task.CompletedTask;
        }

        var existing = translationProvider.Get()
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationResourceStringID), stringRow.ResourceStringID)
            .WhereEquals(nameof(ResourceStringTranslationInfo.ResourceStringTranslationContentLanguageID), languageId)
            .TopN(1)
            .FirstOrDefault();

        if (value is null)
        {
            if (existing is not null)
            {
                translationProvider.Delete(existing);
            }

            return Task.CompletedTask;
        }

        if (existing is null)
        {
            existing = new ResourceStringTranslationInfo
            {
                ResourceStringTranslationResourceStringID = stringRow.ResourceStringID,
                ResourceStringTranslationContentLanguageID = languageId,
                ResourceStringTranslationValue = value,
            };
        }
        else
        {
            existing.ResourceStringTranslationValue = value;
        }

        translationProvider.Set(existing);
        return Task.CompletedTask;
    }
}

internal sealed class ContentLanguageRepository(IInfoProvider<ContentLanguageInfo> provider)
    : IContentLanguageRepository
{
    public Task<bool> ExistsAsync(string code) =>
        Task.FromResult(provider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), code)
            .TopN(1)
            .Any());

    public IReadOnlyList<ContentLanguageOption> ListAll() =>
        provider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageDisplayName))
            .ToArray()
            .Select(l => new ContentLanguageOption(l.ContentLanguageName, l.ContentLanguageDisplayName))
            .ToList();
}
