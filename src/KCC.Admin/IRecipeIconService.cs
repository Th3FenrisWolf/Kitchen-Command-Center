namespace KCC.Admin;

/// <summary>
/// Picks the best curated Font Awesome icon for a recipe. Always returns a valid kebab-case
/// name from <see cref="RecipeIcons.All"/> — never empty.
/// </summary>
public interface IRecipeIconService
{
    /// <summary>Picks the best icon for a recipe.</summary>
    /// <param name="name">Recipe name.</param>
    /// <param name="description">Recipe description.</param>
    /// <param name="ingredients">Ingredient names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A kebab-case icon name from <see cref="RecipeIcons.All"/>.</returns>
    Task<string> PickAsync(string name, string description, IEnumerable<string> ingredients, CancellationToken cancellationToken);
}
