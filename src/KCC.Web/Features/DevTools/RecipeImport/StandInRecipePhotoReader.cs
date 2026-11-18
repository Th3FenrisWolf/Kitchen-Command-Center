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
    private const int PhotosOfAWholeRecipePanel = 4;
    private const int PhotosOfEstimatedServings = 5;
    private const string ProposalReason = "The stand-in reader proposes the first recipe in the catalog.";

    private const string CategoryName = "Breakfast";
    private const string CategoryReason = "Eggs and toast, the kind of dish served at breakfast.";
    private const string StatedTagName = "Vegan";
    private const string SuggestedTagName = "Easy";
    private const string SuggestedTagReason = "Few ingredients and four short steps.";

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

    private static readonly ReaderNutrition PerServingPanel = new(
        "serving",
        [
            new ReaderNutrient("calories", 320, "kcal", "320 kcal", false),
            new ReaderNutrient("protein", 12, "g", "12 g", false),
            new ReaderNutrient("carbohydrates", 30, "g", "30 g", false),
            new ReaderNutrient("fat", 15, "g", "15 g", false),
            new ReaderNutrient("fiber", 1.5m, "g", "1.5 g", false),
        ]);

    private static readonly ReaderNutrition WholeRecipePanel = new(
        "recipe",
        [
            new ReaderNutrient("calories", 8368, "kJ", "8368 kJ", false),
            new ReaderNutrient("protein", 80, "g", "80 g", false),
            new ReaderNutrient("carbohydrates", 160, "g", "160 g", false),
            new ReaderNutrient("fat", 64, "g", "64 g", false),
            new ReaderNutrient("fiber", 16, "g", "16 g", false),
            new ReaderNutrient("salt", 20, "g", "20 g", false),
        ]);

    public bool IsConfigured => true;

    public async Task<ReadAttempt> ReadAsync(ReadRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.StandInDelaySeconds), timeProvider, cancellationToken);
        var read = SkilletCornbread with { Details = DetailsFor(request) };
        var output = request.Photos.Count switch
        {
            PhotosThatProposeR1 when request.Catalog?.Find(FirstShortId) is not null =>
                read with { MatchId = FirstShortId, MatchReason = ProposalReason },
            PhotosThatCopyAVariant when FirstVariantToCopy(request) is { } variant => CopyOf(read, variant),
            PhotosOfEstimatedServings => read with
            {
                Variant = read.Variant with { Servings = new ReaderCount(8, new ReaderMark(MarkKinds.Estimate, "Not printed")) },
            },
            _ => read,
        };
        return ReadAttempt.Read(output, NoTokens());
    }

    private static ReaderDetails DetailsFor(ReadRequest request)
    {
        var taxonomy = request.Taxonomy;
        var wholeRecipe = request.Photos.Count is PhotosOfAWholeRecipePanel or PhotosOfEstimatedServings;
        return new ReaderDetails(
            new ReaderCategory(taxonomy?.CategoryId(CategoryName), false, null, CategoryReason),
            [
                new ReaderTag(taxonomy?.TagId(StatedTagName), true, StatedTagName, null),
                new ReaderTag(taxonomy?.TagId(SuggestedTagName), false, null, new ReaderMark(MarkKinds.Written, SuggestedTagReason)),
            ],
            new ReaderDifficulty("Easy", "easy", null),
            wholeRecipe ? WholeRecipePanel : PerServingPanel);
    }

    private static ReaderOutput CopyOf(ReaderOutput read, VariantRecord variant) => read with
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
        Speed = null,
    };
}
