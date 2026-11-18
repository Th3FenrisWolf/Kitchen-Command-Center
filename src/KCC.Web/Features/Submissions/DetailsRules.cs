using KCC.Web.Features.Api;

namespace KCC.Web.Features.Submissions;

public static class DetailsRules
{
    public const int MaxProposalLength = 100;

    private static readonly string[] Difficulties = ["Easy", "Medium", "Hard"];

    public static string RefusedField(CreateRecipeRequest request) =>
        TooLong(request.ProposedCategory) ? "proposedCategory" : RefusedField(request.FirstVariant);

    public static string RefusedField(CreateVariantRequest request)
    {
        if (request.Difficulty is not null && !Difficulties.Contains(request.Difficulty))
        {
            return "difficulty";
        }

        if (TooLong(request.ProposedTags))
        {
            return "proposedTags";
        }

        var nutrition = request.Nutrition;
        if (nutrition is null)
        {
            return null;
        }

        return OutOfRange(nutrition.Calories, 10_000) ? "calories"
            : OutOfRange(nutrition.ProteinG, 1_000) ? "proteinG"
            : OutOfRange(nutrition.CarbsG, 1_000) ? "carbsG"
            : OutOfRange(nutrition.FatG, 1_000) ? "fatG"
            : OutOfRange(nutrition.SaturatedFatG, 1_000) ? "saturatedFatG"
            : OutOfRange(nutrition.FiberG, 1_000) ? "fiberG"
            : OutOfRange(nutrition.SugarG, 1_000) ? "sugarG"
            : OutOfRange(nutrition.SodiumMg, 50_000) ? "sodiumMg"
            : null;
    }

    private static bool TooLong(string proposal) => proposal?.Trim().Length > MaxProposalLength;

    private static bool OutOfRange(int? value, int max) => value is < 0 || value > max;
}
