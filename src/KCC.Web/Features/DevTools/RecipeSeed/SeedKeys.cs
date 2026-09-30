namespace KCC.Web.Features.DevTools.RecipeSeed;

// The same seed always yields the same key, so a second run finds what the first one created.
public static class SeedKeys
{
    public static Guid Recipe(string recipeName) => DeterministicKey.For($"recipe::{recipeName}");

    public static Guid Variant(string recipeName, string variantName) => DeterministicKey.For($"variant::{recipeName}::{variantName}");

    public static Guid Author(string userName) => DeterministicKey.For($"author::{userName}");

    public static Guid Reviewer(string recipeName, int index) => DeterministicKey.For($"review::{recipeName}::{index}");
}
