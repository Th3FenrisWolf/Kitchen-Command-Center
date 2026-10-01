namespace KCC.Web.Features.DevTools.RecipeSeed;

public sealed record SeedIngredient(string Name, decimal? Quantity, string Unit, bool IsEyeballed = false);

public sealed record SeedInstruction(int Step, string Text);

public sealed record SeedVariant(
    string Name,
    string Description,
    int PrepMinutes,
    int CookMinutes,
    int Servings,
    string Icon,
    string[] Diets,
    SeedIngredient[] Ingredients,
    SeedInstruction[] Instructions);

public sealed record SeedReview(decimal Rating);

public sealed record SeedRecipe(
    string Name,
    string Category,
    string AuthorKey,
    string Description,
    string Icon,
    int PublishedDaysAgo,
    SeedVariant[] Variants,
    SeedReview[] Reviews);

public sealed record SeedAuthor(string Key, string UserName, string Email, string FirstName, string LastName);
