// KCC.Web does not enable nullable globally; this file opts in so the response
// and request DTOs can express semantically meaningful nullability on Value.
#nullable enable

using System.ComponentModel.DataAnnotations;

namespace KCC.Web.Features.ResourceStringEditing;

public sealed record ResourceStringValueResponse(
    string Key,
    string Language,
    string? Value,
    string FallbackValue,
    bool Exists);

public sealed class ResourceStringUpsertRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Key { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Language { get; init; } = string.Empty;

    // No StringLength: ResourceString[Translation]Value is NVARCHAR(MAX) (longtext).
    public string? Value { get; init; }
}

public sealed record ResourceStringUpsertResponse(string Key, string Language, string Value);
