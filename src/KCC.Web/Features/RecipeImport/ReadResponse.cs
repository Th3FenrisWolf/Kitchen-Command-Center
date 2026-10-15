using Anthropic.Models.Messages;

namespace KCC.Web.Features.RecipeImport;

public sealed record ReadResponse(
    string Outcome,
    RecipeValues Recipe,
    VariantValues Variant,
    IReadOnlyList<IngredientValue> Ingredients,
    IReadOnlyList<InstructionValue> Instructions,
    RecipeMatch Match,
    PossibleDuplicate PossibleDuplicate,
    IReadOnlyList<string> OtherRecipes,
    IReadOnlyList<RecipeChoice> Recipes);

public sealed record RecipeValues(MarkedValue<string> Name, MarkedValue<string> Description);

public sealed record VariantValues(
    MarkedValue<string> Name,
    MarkedValue<string> Description,
    MarkedValue<int?> PrepTime,
    MarkedValue<int?> CookTime,
    MarkedValue<int?> Servings);

public sealed record MarkedValue<T>(T Value, Mark Mark);

public sealed record IngredientValue(string Name, decimal? Quantity, string Unit, bool IsEyeballed, Mark Mark);

public sealed record InstructionValue(string Text, Mark Mark);

public sealed record RecipeMatch(Guid RecipeKey, string RecipeName, string Reason);

public sealed record PossibleDuplicate(Guid VariantKey, string VariantName, string RecipeName);

public sealed record RecipeChoice(Guid Key, string Name);

public sealed record Mark(string Kind, string Reason, string Code, string Arg);

public static class ReadOutcomes
{
    public const string Recipe = "recipe";
    public const string NoRecipe = "noRecipe";
}

public static class MarkKinds
{
    public const string Estimate = "estimate";
    public const string Unclear = "unclear";
    public const string Written = "written";
    public const string Translated = "translated";
}

public static class MarkCodes
{
    public const string Shortened = "shortened";
    public const string OutOfRange = "outOfRange";
    public const string Translated = "translated";
}

public static class ImportFlows
{
    public const string Recipe = "recipe";
    public const string Variant = "variant";
}

public sealed record ReadRequest(string Flow, Guid? RecipeKey, IReadOnlyList<ReadPhoto> Photos, RecipeCatalog Catalog);

public sealed record ReadPhoto(string MediaType, byte[] Data);

public sealed record ReadAttempt
{
    private ReadAttempt(ReaderOutput output, Usage usage, ReadFailure? failure)
    {
        Output = output;
        Usage = usage;
        Failure = failure;
    }

    public ReaderOutput Output { get; }

    public Usage Usage { get; }

    public ReadFailure? Failure { get; }

    public bool ServiceAnswered => Failure is not ReadFailure.NoAnswer and not ReadFailure.TimedOut;

    public static ReadAttempt Read(ReaderOutput output, Usage usage) => new(output, usage, null);

    public static ReadAttempt Refused(Usage usage) => new(null, usage, ReadFailure.Refused);

    public static ReadAttempt CutOff(Usage usage) => new(null, usage, ReadFailure.CutOff);

    public static ReadAttempt Unusable(Usage usage) => new(null, usage, ReadFailure.Unusable);

    public static ReadAttempt ServiceError() => new(null, null, ReadFailure.ServiceError);

    public static ReadAttempt NoAnswer() => new(null, null, ReadFailure.NoAnswer);

    public static ReadAttempt TimedOut() => new(null, null, ReadFailure.TimedOut);
}

public enum ReadFailure
{
    Refused,
    CutOff,
    Unusable,
    ServiceError,
    NoAnswer,
    TimedOut,
}

public sealed record ReaderOutput(
    bool Found,
    ReaderRecipe Recipe,
    ReaderVariant Variant,
    IReadOnlyList<ReaderIngredient> Ingredients,
    IReadOnlyList<ReaderStep> Steps,
    string MatchId,
    string MatchReason,
    IReadOnlyList<string> OtherRecipes,
    string SourceLanguage);

public sealed record ReaderRecipe(ReaderText Name, ReaderText Description);

public sealed record ReaderVariant(
    ReaderText Name,
    ReaderText Description,
    ReaderCount PrepMinutes,
    ReaderCount CookMinutes,
    ReaderCount Servings);

public sealed record ReaderText(string Value, ReaderMark Mark);

public sealed record ReaderCount(decimal? Value, ReaderMark Mark);

public sealed record ReaderIngredient(string Name, decimal? Quantity, string Unit, bool ToTaste, ReaderMark Mark);

public sealed record ReaderStep(string Text, ReaderMark Mark);

public sealed record ReaderMark(string Kind, string Reason);
