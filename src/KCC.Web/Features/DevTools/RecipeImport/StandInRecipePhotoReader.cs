using Anthropic.Models.Messages;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.Options;
using JsonCollections = KCC.Web.Features.Helpers.JsonSerializer;

namespace KCC.Web.Features.DevTools.RecipeImport;

public sealed class StandInRecipePhotoReader(
    IOptionsMonitor<RecipeImportOptions> options,
    TimeProvider timeProvider,
    IServiceScopeFactory scopes) : IRecipePhotoReader
{
    private const int PhotosThatProposeR1 = 2;
    private const int PhotosThatCopyAVariant = 3;
    private const string FirstShortId = "R1";
    private const string ProposalReason = "The stand-in reader proposes the first recipe in the catalog.";

    private static readonly ReaderMark NotInThePhotos = new(MarkKinds.Written, "Not in the photos");

    private static readonly ReaderOutput SkilletCornbread = new(
        Found: true,
        new ReaderRecipe(
            new ReaderText("Skillet Cornbread", null),
            new ReaderText("Golden cornbread with crisp edges, baked in a cast-iron skillet.", NotInThePhotos)),
        new ReaderVariant(
            new ReaderText("Honey Butter", null),
            new ReaderText("Brushed with honey butter straight from the oven.", NotInThePhotos),
            new ReaderCount(15, new ReaderMark(MarkKinds.Estimate, "Not printed")),
            new ReaderCount(25, null),
            new ReaderCount(8, null)),
        [
            new ReaderIngredient("Cornmeal", 1, "cup", false, null),
            new ReaderIngredient("Flour", 1, "cup", false, null),
            new ReaderIngredient("Buttermilk", 1, "cup", false, new ReaderMark(MarkKinds.Unclear, "Printed as 1 to 1 1/4")),
            new ReaderIngredient("Eggs", 2, "whole", false, null),
            new ReaderIngredient("Butter", 4, "tbsp", false, null),
            new ReaderIngredient("Honey", 2, "tbsp", false, null),
            new ReaderIngredient("Salt", null, string.Empty, true, null),
        ],
        [
            new ReaderStep("Heat the oven to 425°F with the skillet inside.", null),
            new ReaderStep("Whisk the cornmeal, flour and salt, then stir in the buttermilk and eggs.", null),
            new ReaderStep("Pour into the hot buttered skillet and bake for 25 minutes.", null),
            new ReaderStep("Brush with the rest of the butter mixed with the honey.", null),
        ],
        MatchId: null,
        MatchReason: null,
        OtherRecipes: [],
        SourceLanguage: null);

    public bool IsConfigured => true;

    public async Task<ReadAttempt> ReadAsync(ReadRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.StandInDelaySeconds), timeProvider, cancellationToken);
        var output = request.Photos.Count switch
        {
            PhotosThatProposeR1 when request.Catalog?.Find(FirstShortId) is not null =>
                SkilletCornbread with { MatchId = FirstShortId, MatchReason = ProposalReason },
            PhotosThatCopyAVariant when FirstVariantToCopy(request) is { } variant => CopyOf(variant),
            _ => SkilletCornbread,
        };
        return ReadAttempt.Read(output, NoTokens());
    }

    private static ReaderOutput CopyOf(VariantRecord variant) => SkilletCornbread with
    {
        Ingredients =
        [
            .. JsonCollections.DeserializeCollection<IngredientViewModel>(variant.IngredientsJson)
                .Select(row => new ReaderIngredient(row.Name, row.Quantity, row.Unit ?? string.Empty, row.IsEyeballed, null)),
        ],
        Steps =
        [
            .. JsonCollections.DeserializeCollection<InstructionViewModel>(variant.InstructionsJson)
                .Select(row => new ReaderStep(row.Text, null)),
        ],
    };

    private VariantRecord FirstVariantToCopy(ReadRequest request)
    {
        if (request.Flow != ImportFlows.Variant)
        {
            return request.Catalog?.Find(FirstShortId)?.Page.Variants.FirstOrDefault();
        }

        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IRecipeQueries>()
            .GetPublishedRecipes()
            .FirstOrDefault(page => page.Recipe.Key == request.RecipeKey)
            ?.Variants.FirstOrDefault();
    }

    private static Usage NoTokens() => new()
    {
        InputTokens = 0,
        OutputTokens = 0,
        CacheCreationInputTokens = null,
        CacheReadInputTokens = null,
        CacheCreation = null,
        InferenceGeo = null,
        OutputTokensDetails = null,
        ServerToolUse = null,
        ServiceTier = null,
    };
}
