#nullable enable

namespace KCC.ResourceStrings.Editing;

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
    public ResourceStringUpsertResponse Upsert(ResourceStringUpsertRequest request)
    {
        if (!languageRepo.Exists(request.Language))
        {
            throw new InvalidLanguageException(request.Language);
        }

        var isDefault = string.Equals(request.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase);

        if (isDefault)
        {
            var value = request.Value ?? string.Empty;
            writeRepo.UpsertString(request.Key, value);
            return new ResourceStringUpsertResponse(request.Key, request.Language, value);
        }

        if (!writeRepo.StringExists(request.Key))
        {
            try
            {
                writeRepo.UpsertString(request.Key, string.Empty);
            }
            catch (Exception) when (writeRepo.StringExists(request.Key))
            {
                // Another thread created the row concurrently.
            }
        }

        writeRepo.UpsertTranslation(request.Key, request.Language, request.Value);
        return new ResourceStringUpsertResponse(request.Key, request.Language, request.Value ?? string.Empty);
    }
}
