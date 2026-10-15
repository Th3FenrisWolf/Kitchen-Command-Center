using KCC.Web.Features.RecipeImport;

namespace KCC.ReaderEval;

public interface IEvalService
{
    Task<TimedAttempt> ReadAsync(
        string model,
        IReadOnlyList<ReadPhoto> photos,
        RecipeCatalog catalog,
        CancellationToken cancellationToken);

    Task<TokenCount> CountTokensAsync(
        string model,
        IReadOnlyList<ReadPhoto> photos,
        RecipeCatalog catalog,
        CancellationToken cancellationToken);
}

public sealed record TimedAttempt(ReadAttempt Attempt, TimeSpan Elapsed, string ServiceError);

public sealed record TokenCount(long? InputTokens, string Error);
