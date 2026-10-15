using System.Globalization;
using System.Text;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.Recipes;
using JsonCollections = KCC.Web.Features.Helpers.JsonSerializer;

namespace KCC.Web.Features.RecipeImport;

public static class DuplicateVariantCheck
{
    private const double LeastOverlap = 0.8;
    private const int MostStepDifference = 2;
    private const int ShortestWordThatLosesItsS = 4;

    public static PossibleDuplicate Find(
        IReadOnlyList<string> ingredientNames,
        int stepCount,
        IEnumerable<RecipePageData> recipes)
    {
        var readNames = NormalizedNames(ingredientNames);
        if (readNames.Count == 0)
        {
            return null;
        }

        PossibleDuplicate closest = null;
        var highestOverlap = 0.0;
        foreach (var page in recipes)
        {
            foreach (var variant in page.Variants)
            {
                var overlap = Overlap(readNames, NormalizedNames(IngredientNames(variant)));
                if (overlap >= LeastOverlap
                    && overlap > highestOverlap
                    && Math.Abs(StepCount(variant) - stepCount) <= MostStepDifference)
                {
                    highestOverlap = overlap;
                    closest = new PossibleDuplicate(variant.Key, variant.Name, page.Recipe.Name);
                }
            }
        }

        return closest;
    }

    private static List<string> IngredientNames(VariantRecord variant) =>
        [.. JsonCollections.DeserializeCollection<IngredientViewModel>(variant.IngredientsJson).Select(row => row?.Name)];

    private static int StepCount(VariantRecord variant) =>
        JsonCollections.DeserializeCollection<InstructionViewModel>(variant.InstructionsJson)
            .Count(step => !string.IsNullOrWhiteSpace(step?.Text));

    private static double Overlap(HashSet<string> read, HashSet<string> stored)
    {
        var union = read.Union(stored).Count();
        return union == 0 ? 0 : (double)read.Intersect(stored).Count() / union;
    }

    private static HashSet<string> NormalizedNames(IEnumerable<string> names) =>
        [.. names.Select(Normalized).Where(name => name.Length > 0)];

    private static string Normalized(string name)
    {
        var letters = new StringBuilder();
        foreach (var character in (name ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsWhiteSpace(character) || character == '/'
                || char.GetUnicodeCategory(character) == UnicodeCategory.DashPunctuation)
            {
                _ = letters.Append(' ');
            }
            else if (!char.IsPunctuation(character) && !char.IsSymbol(character))
            {
                _ = letters.Append(character);
            }
        }

        return string.Join(
            ' ',
            letters.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Length >= ShortestWordThatLosesItsS && word.EndsWith('s') ? word[..^1] : word));
    }
}
