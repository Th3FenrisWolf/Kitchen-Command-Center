using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.RecipeImport;

[Route("api/recipe-imports")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Imports)]
public class RecipeImportApiController(
    IMemberManager memberManager,
    IRecipePhotoReader reader,
    IRecipeQueries recipes,
    IImportSpendLedger ledger,
    IOptionsMonitor<RecipeImportOptions> options,
    TimeProvider timeProvider) : ControllerBase
{
    private const long MaxRequestBytes = 25_000_000;

    [HttpPost("read")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Read(
        [FromForm] List<IFormFile> photos,
        [FromForm] string flow,
        [FromForm] string recipeKey,
        CancellationToken cancellationToken)
    {
        if (await memberManager.GetCurrentMemberAsync() is null)
        {
            return Unauthorized();
        }

        if (!reader.IsConfigured)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, "Recipe import from photos is off.", "unavailable");
        }

        if (!ModelState.IsValid)
        {
            return InvalidPhotos();
        }

        if (!IsValidRequest(flow, recipeKey, out var variantOf))
        {
            return Error(
                StatusCodes.Status400BadRequest,
                "The flow must be recipe, or variant with a recipe key.",
                "invalidRequest");
        }

        if (await PhotoValidation.ReadAsync(photos, cancellationToken) is not { } photosToRead)
        {
            return InvalidPhotos();
        }

        var chosen = variantOf is { } key ? recipes.FindPublishedRecipe(key) : null;
        if (variantOf is not null && chosen is null)
        {
            return Error(StatusCodes.Status404NotFound, "Recipe not found.", "recipeNotFound");
        }

        var published = recipes.GetPublishedRecipes();
        var catalog = chosen is null ? RecipeCatalog.From(published) : null;
        var duplicateCandidates = catalog?.Entries.Select(entry => entry.Page)
            ?? published.Where(page => page.Recipe.Key == chosen.Key);
        var model = options.CurrentValue.Model;
        var profile = ModelProfiles.Find(model)
            ?? throw new InvalidOperationException($"RecipeImport:Model {model} has no price profile.");
        var now = timeProvider.GetUtcNow();
        var reservationMicroUsd = ReadCost.ReservationMicroUsd(profile, photosToRead.Count, catalog?.Characters ?? 0);
        if (!ledger.TryReserve(ImportSpendLedger.MonthOf(now), reservationMicroUsd, out var reservation))
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                error = "This month's photo imports have reached their spending ceiling.",
                code = "ceilingReached",
                availableAgainOn = ImportAvailability.AvailableAgainOn(now),
            });
        }

        // The service bills a read whose member has gone, so the read and its settlement outlive the request.
        var attempt = await reader.ReadAsync(
            new ReadRequest(flow, chosen?.Key, photosToRead, catalog),
            CancellationToken.None);
        if (attempt.ServiceAnswered)
        {
            ledger.Settle(reservation, attempt.Usage is { } usage ? ReadCost.MicroUsd(profile, usage) : 0);
        }

        return attempt.Failure switch
        {
            null => Ok(ReadResponseOf(attempt.Output, catalog, duplicateCandidates)),
            ReadFailure.TimedOut =>
                Error(StatusCodes.Status504GatewayTimeout, "Reading the photos took too long.", "readTimedOut"),
            _ => Error(StatusCodes.Status502BadGateway, "The photos could not be read.", "readFailed"),
        };
    }

    private static ReadResponse ReadResponseOf(
        ReaderOutput output,
        RecipeCatalog catalog,
        IEnumerable<RecipePageData> duplicateCandidates)
    {
        var read = ReadResultNormalizer.Normalize(output);
        if (read.Outcome == ReadOutcomes.NoRecipe)
        {
            return read;
        }

        return read with
        {
            Match = catalog?.Match(output.MatchId, output.MatchReason),
            Recipes = catalog?.Choices,
            PossibleDuplicate = DuplicateVariantCheck.Find(
                [.. read.Ingredients.Select(ingredient => ingredient.Name)],
                read.Instructions.Count,
                duplicateCandidates),
        };
    }

    private static bool IsValidRequest(string flow, string recipeKey, out Guid? variantOf)
    {
        variantOf = null;
        if (flow == ImportFlows.Recipe)
        {
            return true;
        }

        if (flow == ImportFlows.Variant && Guid.TryParse(recipeKey, out var key))
        {
            variantOf = key;
            return true;
        }

        return false;
    }

    private ObjectResult InvalidPhotos() => Error(
        StatusCodes.Status400BadRequest,
        "The photos must be 1 to 10 JPEG, PNG, WebP or GIF files within the size limits.",
        "invalidPhotos");

    private ObjectResult Error(int status, string error, string code) => StatusCode(status, new { error, code });
}
