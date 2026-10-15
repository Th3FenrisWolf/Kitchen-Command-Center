namespace KCC.Web.Features.RecipeImport;

public interface IRecipePhotoReader
{
    bool IsConfigured { get; }

    Task<ReadAttempt> ReadAsync(ReadRequest request, CancellationToken cancellationToken);
}
