using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.RecipeImport;

public static class ReadResultNormalizer
{
    private const int NameLimit = 255;
    private const int DescriptionLimit = 2_000;
    private const int IngredientNameLimit = 200;
    private const int UnitLimit = 50;
    private const int StepLimit = 2_000;
    private const int MaxIngredients = 100;
    private const int MaxSteps = 60;
    private const int MaxMinutes = 10_080;
    private const int MaxServings = 1_000;
    private const decimal MaxQuantity = 100_000m;
    private const int QuantityDecimals = 3;
    private const string English = "English";

    private static readonly Mark Shortened = new(MarkKinds.Unclear, null, MarkCodes.Shortened, null);
    private static readonly Mark OutOfRange = new(MarkKinds.Unclear, null, MarkCodes.OutOfRange, null);

    private static readonly string[] KindsByPrecedence =
    [
        MarkKinds.Unclear,
        MarkKinds.Estimate,
        MarkKinds.Suggested,
        MarkKinds.Calculated,
        MarkKinds.Written,
        MarkKinds.Translated,
    ];

    private static readonly string[] DifficultyLevels = ["Easy", "Medium", "Hard"];

    private static readonly string[] KindsTheModelSets = [MarkKinds.Estimate, MarkKinds.Unclear, MarkKinds.Written];

    public static ReadResponse Normalize(ReaderOutput output, ReaderTaxonomy taxonomy = null, string flow = ImportFlows.Recipe)
    {
        var ingredients = Ingredients(output?.Ingredients);
        var instructions = Instructions(output?.Steps);
        if (output is not { Found: true } || (ingredients.Count == 0 && instructions.Count == 0))
        {
            return NoRecipe();
        }

        var translated = TranslatedFrom(output.SourceLanguage);
        var servings = Count(output.Variant?.Servings, 1, MaxServings);
        return new ReadResponse(
            ReadOutcomes.Recipe,
            new RecipeValues(
                Text(output.Recipe?.Name, NameLimit, translated),
                Text(output.Recipe?.Description, DescriptionLimit, translated)),
            new VariantValues(
                Text(output.Variant?.Name, NameLimit, translated),
                Text(output.Variant?.Description, DescriptionLimit, translated),
                Count(output.Variant?.PrepMinutes, 0, MaxMinutes),
                Count(output.Variant?.CookMinutes, 0, MaxMinutes),
                servings),
            [.. ingredients.Select(ingredient => ingredient with { Mark = ingredient.Mark ?? translated })],
            [.. instructions.Select(instruction => instruction with { Mark = instruction.Mark ?? translated })],
            Match: null,
            PossibleDuplicate: null,
            OtherRecipes: Names(output.OtherRecipes),
            Recipes: null,
            Details: Details(output.Details, taxonomy, flow, servings));
    }

    private static ReadResponse NoRecipe() => new(
        ReadOutcomes.NoRecipe,
        new RecipeValues(new(string.Empty, null), new(string.Empty, null)),
        new VariantValues(
            new(string.Empty, null),
            new(string.Empty, null),
            new(null, null),
            new(null, null),
            new(null, null)),
        [],
        [],
        Match: null,
        PossibleDuplicate: null,
        OtherRecipes: [],
        Recipes: null,
        Details: EmptyDetails());

    private static DetailsValues EmptyDetails() =>
        new(new(null, null), [], new(null, null), NutritionConversion.Empty());

    private static DetailsValues Details(
        ReaderDetails details,
        ReaderTaxonomy taxonomy,
        string flow,
        MarkedValue<int?> servings) =>
        details is null
            ? EmptyDetails()
            : new DetailsValues(
                flow == ImportFlows.Variant ? new(null, null) : Category(details.Category, taxonomy),
                Tags(details.Tags, taxonomy),
                Difficulty(details.Difficulty),
                NutritionConversion.PerServing(details.Nutrition, servings));

    private static MarkedValue<Guid?> Category(ReaderCategory read, ReaderTaxonomy taxonomy)
    {
        if (taxonomy?.Category(read?.Id) is not { } category || (!read.Stated && IsBlank(read.Reason)))
        {
            return new(null, null);
        }

        var mark = read.Stated ? NamedAs(category.Name, read.Printed) : Suggested(read.Reason);
        return new(category.Key, mark);
    }

    private static List<TagValue> Tags(IReadOnlyList<ReaderTag> rows, ReaderTaxonomy taxonomy) =>
    [
        .. (rows ?? [])
            .Select(row => (Row: row, Choice: taxonomy?.Tag(row?.Id)))
            .Where(tag => tag.Choice is not null
                && (tag.Row.Stated || (tag.Choice.Kind != TagKinds.Diet && !IsBlank(tag.Row.Mark?.Reason))))
            .DistinctBy(tag => tag.Choice.Key)
            .Select(tag => new TagValue(tag.Choice.Key, TagMark(tag.Row, tag.Choice))),
    ];

    private static Mark TagMark(ReaderTag row, TagChoice choice)
    {
        var model = ModelMark(row.Mark);
        var server = row.Stated ? NamedAs(choice.Name, row.Printed) : Suggested(model?.Reason);
        return server is null ? model : Preferred(model, server);
    }

    private static MarkedValue<string> Difficulty(ReaderDifficulty read)
    {
        var level = DifficultyLevels.FirstOrDefault(name => string.Equals(name, read?.Level, StringComparison.OrdinalIgnoreCase));
        var stated = !IsBlank(read?.Printed);
        if (level is null || (!stated && IsBlank(read.Reason)))
        {
            return new(null, null);
        }

        return new(level, stated ? NamedAs(level, read.Printed) : Suggested(read.Reason));
    }

    private static Mark NamedAs(string listName, string printed)
    {
        var word = printed?.Trim();
        return string.IsNullOrEmpty(word) || string.Equals(word, listName, StringComparison.OrdinalIgnoreCase)
            ? null
            : new Mark(MarkKinds.Suggested, null, MarkCodes.PrintedAs, word);
    }

    private static Mark Suggested(string reason) => new(MarkKinds.Suggested, reason.Trim(), null, null);

    private static bool IsBlank(string text) => string.IsNullOrWhiteSpace(text);

    private static List<IngredientValue> Ingredients(IReadOnlyList<ReaderIngredient> rows) =>
    [
        .. (rows ?? []).Where(row => !string.IsNullOrWhiteSpace(row?.Name)).Take(MaxIngredients).Select(Ingredient),
    ];

    private static IngredientValue Ingredient(ReaderIngredient row)
    {
        var (name, mark) = Fitted(row.Name, IngredientNameLimit, ModelMark(row.Mark));
        (var unit, mark) = Fitted(row.Unit, UnitLimit, mark);
        var quantity = row.Quantity is { } read
            ? decimal.Round(read, QuantityDecimals, MidpointRounding.AwayFromZero)
            : (decimal?)null;
        if (quantity is { } rounded && (rounded <= 0 || rounded > MaxQuantity))
        {
            quantity = null;
            mark = Preferred(mark, OutOfRange);
        }

        return new IngredientValue(name, quantity, unit, row.ToTaste || quantity is null, mark);
    }

    private static List<InstructionValue> Instructions(IReadOnlyList<ReaderStep> rows) =>
    [
        .. (rows ?? [])
            .Where(row => !string.IsNullOrWhiteSpace(row?.Text))
            .Take(MaxSteps)
            .Select(row => Fitted(row.Text, StepLimit, ModelMark(row.Mark)))
            .Select(step => new InstructionValue(step.Value, step.Mark)),
    ];

    private static List<string> Names(IReadOnlyList<string> names) =>
        [.. (names ?? []).Select(name => name?.Trim()).Where(name => !string.IsNullOrEmpty(name))];

    private static MarkedValue<string> Text(ReaderText text, int limit, Mark translated)
    {
        var (value, mark) = Fitted(text?.Value, limit, ModelMark(text?.Mark));
        return new MarkedValue<string>(value, value.Length > 0 ? mark ?? translated : mark);
    }

    private static MarkedValue<int?> Count(ReaderCount count, int lowest, int highest)
    {
        var mark = ModelMark(count?.Mark);
        return count?.Value switch
        {
            null => new MarkedValue<int?>(null, mark),
            { } value when decimal.IsInteger(value) && value >= lowest && value <= highest =>
                new MarkedValue<int?>((int)value, mark),
            _ => new MarkedValue<int?>(null, Preferred(mark, OutOfRange)),
        };
    }

    private static (string Value, Mark Mark) Fitted(string text, int limit, Mark mark)
    {
        var value = text?.Trim() ?? string.Empty;
        return value.Length <= limit ? (value, mark) : (Cut(value, limit), Preferred(mark, Shortened));
    }

    private static string Cut(string value, int limit)
    {
        var length = char.IsHighSurrogate(value[limit - 1]) ? limit - 1 : limit;
        return value[..length].TrimEnd();
    }

    private static Mark ModelMark(ReaderMark mark) => mark is null
        ? null
        : new Mark(
            KindsTheModelSets.Contains(mark.Kind) ? mark.Kind : MarkKinds.Unclear,
            string.IsNullOrWhiteSpace(mark.Reason) ? null : mark.Reason.Trim(),
            null,
            null);

    private static Mark TranslatedFrom(string sourceLanguage)
    {
        var language = sourceLanguage?.Trim();
        return string.IsNullOrEmpty(language) || string.Equals(language, English, StringComparison.OrdinalIgnoreCase)
            ? null
            : new Mark(MarkKinds.Translated, null, MarkCodes.Translated, language);
    }

    private static Mark Preferred(Mark current, Mark server) =>
        current is null || Array.IndexOf(KindsByPrecedence, server.Kind) <= Array.IndexOf(KindsByPrecedence, current.Kind)
            ? server
            : current;
}
