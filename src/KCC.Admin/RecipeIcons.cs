namespace KCC.Admin;

/// <summary>
/// The curated set of Font Awesome Pro duotone icons available for recipes, as full
/// Font Awesome class strings (e.g. "fa-duotone fa-cheese"). This is the single
/// source of truth shared by the AI enum, the deterministic fallback, and the backoffice's
/// recipe icon editor; the value is stored verbatim and rendered as-is.
/// </summary>
public static class RecipeIcons
{
    public static readonly IReadOnlyList<string> All = """
        aeropress apple-whole bacon bagel baguette beer-mug beer-mug-empty blender bone bottle-baby
        bottle-droplet bottle-water bowl-chopsticks bowl-chopsticks-noodles bowl-food bowl-hot bowl-rice
        bowl-scoop bowl-scoops bowl-soft-serve bowl-spoon bread-loaf bread-slice bread-slice-butter burger
        burger-cheese burger-fries burger-glass burger-lettuce burger-soda burrito butter cake-candles
        cake-slice can-food candy candy-bar candy-cane candy-corn carrot champagne-glass champagne-glasses
        cheese cheese-swiss chemex chopsticks circle-gf cloud-meatball coffee-bean coffee-beans coffee-pot
        cookie corn crab crate-apple croissant cubes-stacked cup-straw cup-straw-swoosh cup-togo cupcake custard
        donut drumstick drumstick-bite egg egg-fried falafel fish fish-bones fish-cooked fish-fins flask
        flask-round-poison flask-round-potion flatbread flatbread-stuffed fondue-pot french-fries
        gingerbread-man glass glass-citrus glass-empty glass-half glass-water glass-water-droplet hat-chef
        honey-pot hotdog ice-cream jar jar-wheat jug jug-bottle lemon lobster lollipop lychee martini-glass
        martini-glass-citrus martini-glass-empty meat mug mug-hot mug-marshmallows mug-saucer mug-tea
        mug-tea-saucer pan-food pan-frying pancakes pepper-hot pie pizza pizza-slice plate-wheat popcorn
        popsicle pot-food pretzel pumpkin salad salt-shaker sandwich sausage seedling shish-kebab shrimp
        soft-serve squid steak stroopwafel sushi sushi-roll table-bar table-dining taco tamale truck-utensils
        turkey user-chef user-chef-hair-long waffle wheat wheat-awn wheat-awn-circle-exclamation wheat-awn-slash
        wheat-slash whiskey-glass whiskey-glass-ice wine-bottle wine-glass wine-glass-empty
        """
        .Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(name => $"fa-duotone fa-{name}")
        .ToArray();

    /// <summary>
    /// Deterministically maps a seed (e.g. the recipe name) to one icon. Stable across calls,
    /// so a recipe always gets the same fallback icon and never an empty value.
    /// </summary>
    /// <param name="seed">The seed value, typically the recipe name.</param>
    /// <returns>A full Font Awesome class string from <see cref="All"/>.</returns>
    public static string Fallback(string seed)
    {
        int hash = 0;
        foreach (char c in seed ?? string.Empty)
        {
            hash = unchecked((hash * 31) + c);
        }

        int index = Math.Abs(hash % All.Count);
        return All[index];
    }

    public static bool IsKnown(string icon) => icon is not null && All.Contains(icon);
}
