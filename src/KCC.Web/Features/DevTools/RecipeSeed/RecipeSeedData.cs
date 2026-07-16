namespace KCC.Web.Features.DevTools.RecipeSeed;

/// <summary>
/// The recipe search test-data set (25 recipes). Deliberately authored as a coverage matrix over every
/// search path so each is independently assertable:
/// <list type="bullet">
/// <item>Free-text: distinctive tokens isolated to one field each — "tahini" (one recipe's ingredient
///   only), "midnight" (one recipe's description only), "Salazar" (author name only — matches Diego
///   Salazar's two recipes, Shakshuka + Cold Brew Concentrate); "zephyr" appears ONLY in a variant
///   instruction, which is NOT indexed, so it must return nothing.</item>
/// <item>Category facet: all six categories, each on ≥3 recipes (only a recipe's first category is indexed).</item>
/// <item>Diet facet: all eight diets, several recipes multi-diet, some sharing a diet; "Spicy Ramen Flight"
///   spreads different diets across its variants to exercise the union-across-variants facet.</item>
/// <item>Fastest-time range: values at 0 (no variants), 5, 10, exactly 60, and one 90 (&gt; the 60-min
///   slider cap, reachable only with the filter inactive); "Weeknight Tacos" has a slow + fast variant so
///   the faster one sets the indexed time.</item>
/// <item>Sorts: distinct average ratings (with a single clear 5.0 top) and unrated recipes; variant counts
///   of 0–4; and staggered publish dates.</item>
/// <item>Spotlight: "Legendary Lasagna" is the sole 5.0; every Beverage is unrated, so filtering to
///   Beverage yields results with no spotlight.</item>
/// <item>Pagination: 25 recipes &gt; the page size of 12.</item>
/// </list>
/// </summary>
public static class RecipeSeedData
{
    // Category taxonomy (RecipeCategories). Only the FIRST category on a recipe is indexed, so each recipe
    // carries exactly one; covering N categories therefore takes N recipes.
    public const string Breakfast = "Breakfast";
    public const string Lunch = "Lunch";
    public const string Dinner = "Dinner";
    public const string Dessert = "Dessert";
    public const string Snack = "Snack";
    public const string Beverage = "Beverage";

    public static readonly string[] Categories = [Breakfast, Lunch, Dinner, Dessert, Snack, Beverage];

    // Diet taxonomy (RecipeTags) — multi-valued across a recipe's variants.
    public const string Vegetarian = "Vegetarian";
    public const string Vegan = "Vegan";
    public const string GlutenFree = "Gluten-Free";
    public const string DairyFree = "Dairy-Free";
    public const string Keto = "Keto";
    public const string HighProtein = "High-Protein";
    public const string LowCarb = "Low-Carb";
    public const string Spicy = "Spicy";

    public static readonly string[] Diets =
        [Vegetarian, Vegan, GlutenFree, DairyFree, Keto, HighProtein, LowCarb, Spicy];

    public const string AuthorPriya = "priya";
    public const string AuthorDiego = "diego";

    public static readonly SeedAuthor[] Authors =
    [
        new(AuthorPriya, "priya.balan", "priya.balan@seed.kcc.test", "Priya", "Balan"),
        new(AuthorDiego, "diego.salazar", "diego.salazar@seed.kcc.test", "Diego", "Salazar"),
    ];

    public static readonly SeedRecipe[] Recipes =
    [
        // ---- Breakfast (4) --------------------------------------------------------------------------
        new SeedRecipe("Fluffy Buttermilk Pancakes", Breakfast, null,
            "Tall, tender stacks for a lazy weekend morning.", "fa-duotone fa-pancakes", 3,
            [new SeedVariant("Classic Stack", "Griddle to golden.", 10, 15, 4, "fa-duotone fa-pancakes",
                [Vegetarian],
                [new SeedIngredient("Flour", 2, "cups"), new SeedIngredient("Buttermilk", 1.5m, "cups"), new SeedIngredient("Eggs", 1, "whole")],
                [new SeedInstruction(1, "Whisk the batter."), new SeedInstruction(2, "Griddle until bubbles pop.")])],
            [new(4.5m), new(4m)]),

        new SeedRecipe("Avocado Toast Supreme", Breakfast, null,
            "Five minutes, zero cooking, endless smugness.", "fa-duotone fa-bread-slice", 5,
            [new SeedVariant("The Basic", "Smash, season, done.", 5, 0, 1, "fa-duotone fa-bread-slice",
                [Vegan],
                [new SeedIngredient("Sourdough", 2, "slices"), new SeedIngredient("Avocado", 1, "whole"), new SeedIngredient("Chili Flakes", null, "pinch", true)],
                [new SeedInstruction(1, "Toast the bread."), new SeedInstruction(2, "Smash the avocado on top.")])],
            []),

        new SeedRecipe("Shakshuka", Breakfast, AuthorDiego,
            "Eggs poached in a spiced tomato skillet.", "fa-duotone fa-egg-fried", 7,
            [new SeedVariant("Harissa Shakshuka", "Bring the heat.", 10, 25, 3, "fa-duotone fa-egg-fried",
                [Vegetarian, Spicy, GlutenFree],
                [new SeedIngredient("Eggs", 4, "whole"), new SeedIngredient("Crushed Tomato", 1, "can"), new SeedIngredient("Harissa", 1, "tbsp")],
                [new SeedInstruction(1, "Simmer the tomato base."), new SeedInstruction(2, "Crack in the eggs and cover.")])],
            [new(4.5m)]),

        new SeedRecipe("Overnight Oats", Breakfast, null,
            "Assemble at night, grab and go at dawn.", "fa-duotone fa-wheat-awn", 9,
            [new SeedVariant("No-Cook Chia Oats", "Nothing hits the stove.", 5, 0, 1, "fa-duotone fa-wheat-awn",
                [Vegan, HighProtein],
                [new SeedIngredient("Rolled Oats", 0.5m, "cup"), new SeedIngredient("Almond Milk", 0.75m, "cup"), new SeedIngredient("Chia Seeds", 1, "tbsp")],
                [new SeedInstruction(1, "Combine and refrigerate overnight.")])],
            [new(3.5m)]),

        // ---- Lunch (4) ------------------------------------------------------------------------------
        // Ingredient-only token "tahini"; two variants so the faster (15) sets FastestTime below the slower (45).
        new SeedRecipe("Weeknight Bowls", Lunch, null,
            "Fast, flexible grain bowls for busy nights.", "fa-duotone fa-bowl-rice", 12,
            [new SeedVariant("Roasted Veg Bowl", "Sheet-pan vegetables over grains.", 20, 25, 4, "fa-duotone fa-bowl-rice",
                [Vegan, GlutenFree],
                [new SeedIngredient("Chickpeas", 1, "can"), new SeedIngredient("Tahini", 2, "tbsp"), new SeedIngredient("Quinoa", 1, "cup")],
                [new SeedInstruction(1, "Roast the vegetables."), new SeedInstruction(2, "Whisk the tahini sauce.")]),
             new SeedVariant("Quick Hummus Bowl", "No-roast shortcut.", 15, 0, 2, "fa-duotone fa-bowl-rice",
                [Vegetarian],
                [new SeedIngredient("Hummus", 1, "cup"), new SeedIngredient("Pita", 2, "whole")],
                [new SeedInstruction(1, "Assemble everything cold.")])],
            [new(4.5m)]),

        new SeedRecipe("Caprese Sandwich", Lunch, null,
            "Summer on ciabatta, no cooking required.", "fa-duotone fa-sandwich", 14,
            [new SeedVariant("Ciabatta Caprese", "Stack and press.", 10, 0, 2, "fa-duotone fa-sandwich",
                [Vegetarian],
                [new SeedIngredient("Mozzarella", 4, "oz"), new SeedIngredient("Tomato", 1, "whole"), new SeedIngredient("Basil", null, "handful", true)],
                [new SeedInstruction(1, "Layer and drizzle with oil.")])],
            [new(4m)]),

        // Four variants -> top of the "variants" sort; diets spread across variants exercise the union facet.
        new SeedRecipe("Spicy Ramen Flight", Lunch, AuthorPriya,
            "One broth, four wildly different bowls.", "fa-duotone fa-bowl-chopsticks", 6,
            [new SeedVariant("Chili Oil Shoyu", "Bright and fiery.", 10, 10, 1, "fa-duotone fa-bowl-chopsticks",
                [Spicy],
                [new SeedIngredient("Ramen Noodles", 1, "portion"), new SeedIngredient("Chili Oil", 1, "tbsp")],
                [new SeedInstruction(1, "Boil noodles."), new SeedInstruction(2, "Toss with chili oil.")]),
             new SeedVariant("Miso Vegan", "Deep and meat-free.", 15, 15, 1, "fa-duotone fa-bowl-chopsticks",
                [Vegan],
                [new SeedIngredient("Ramen Noodles", 1, "portion"), new SeedIngredient("Miso Paste", 2, "tbsp"), new SeedIngredient("Tofu", 4, "oz")],
                [new SeedInstruction(1, "Whisk miso into broth.")]),
             new SeedVariant("Tonkotsu-Style Protein", "Rich and hearty.", 20, 10, 1, "fa-duotone fa-bowl-chopsticks",
                [HighProtein],
                [new SeedIngredient("Ramen Noodles", 1, "portion"), new SeedIngredient("Pork Belly", 4, "oz"), new SeedIngredient("Soft-Boiled Eggs", 1, "whole")],
                [new SeedInstruction(1, "Warm the broth and add toppings.")]),
             new SeedVariant("Coconut Dairy-Free", "Creamy without the cream.", 12, 8, 1, "fa-duotone fa-bowl-chopsticks",
                [DairyFree],
                [new SeedIngredient("Ramen Noodles", 1, "portion"), new SeedIngredient("Coconut Milk", 0.5m, "cup")],
                [new SeedInstruction(1, "Simmer coconut broth and combine.")])],
            [new(4.5m), new(5m), new(4m)]),

        new SeedRecipe("Quinoa Power Salad", Lunch, null,
            "Make-ahead lunch that keeps its crunch.", "fa-duotone fa-salad", 18,
            [new SeedVariant("Kale & Almond", "Massage the kale, trust us.", 15, 0, 3, "fa-duotone fa-salad",
                [Vegan, GlutenFree, HighProtein],
                [new SeedIngredient("Quinoa", 1, "cup"), new SeedIngredient("Kale", 3, "cups"), new SeedIngredient("Almonds", 0.25m, "cup")],
                [new SeedInstruction(1, "Toss everything with lemon dressing.")])],
            []),

        // ---- Dinner (5) -----------------------------------------------------------------------------
        // Spotlight winner: the single 5.0 average, and the most-recent recipe.
        new SeedRecipe("Legendary Lasagna", Dinner, AuthorPriya,
            "Layered comfort food that reheats like a dream.", "fa-duotone fa-plate-utensils", 1,
            [new SeedVariant("Classic Beef Lasagna", "The one everyone asks for.", 25, 35, 8, "fa-duotone fa-plate-utensils",
                [HighProtein],
                [new SeedIngredient("Lasagna Noodles", 12, "sheets"), new SeedIngredient("Ground Beef", 1, "lb"), new SeedIngredient("Ricotta", 2, "cups")],
                [new SeedInstruction(1, "Brown the beef."), new SeedInstruction(2, "Layer and bake.")])],
            [new(5m), new(5m), new(5m)]),

        // Fastest time 90 (> the 60-min slider cap): only reachable when the time filter is inactive.
        new SeedRecipe("Slow-Braised Short Ribs", Dinner, null,
            "Low, slow, and worth the wait.", "fa-duotone fa-drumstick", 4,
            [new SeedVariant("Red Wine Braise", "Fall-off-the-bone.", 15, 75, 4, "fa-duotone fa-drumstick",
                [HighProtein],
                [new SeedIngredient("Short Ribs", 3, "lb"), new SeedIngredient("Red Wine", 2, "cups"), new SeedIngredient("Carrot", 3, "whole")],
                [new SeedInstruction(1, "Sear the ribs."), new SeedInstruction(2, "Braise low for hours.")])],
            [new(4.5m), new(5m)]),

        new SeedRecipe("Sheet-Pan Salmon", Dinner, null,
            "One pan, weeknight-fast, barely any cleanup.", "fa-duotone fa-fish", 22,
            [new SeedVariant("Lemon Asparagus", "Roast it all together.", 10, 20, 2, "fa-duotone fa-fish",
                [HighProtein, LowCarb, DairyFree],
                [new SeedIngredient("Salmon", 2, "fillets"), new SeedIngredient("Asparagus", 1, "bunch"), new SeedIngredient("Lemon", 1, "whole")],
                [new SeedInstruction(1, "Arrange on a sheet pan."), new SeedInstruction(2, "Roast until flaky.")])],
            [new(4m)]),

        // Fastest time exactly 60 (the slider's upper boundary).
        new SeedRecipe("Hour-Glass Frittata", Dinner, null,
            "A big-batch frittata timed to the hour.", "fa-duotone fa-egg", 26,
            [new SeedVariant("Spinach & Cheese", "Prep 20, bake 40.", 20, 40, 6, "fa-duotone fa-egg",
                [Vegetarian, Keto],
                [new SeedIngredient("Eggs", 10, "whole"), new SeedIngredient("Spinach", 2, "cups"), new SeedIngredient("Cheese", 1, "cup")],
                [new SeedInstruction(1, "Whisk and pour."), new SeedInstruction(2, "Bake until set.")])],
            [new(3m)]),

        // Two variants; the fast bean version (15) sets FastestTime below the slow beef version (45).
        // "zephyr" appears ONLY in an instruction here — it must never be found by search.
        new SeedRecipe("Weeknight Tacos", Dinner, null,
            "Taco night, two ways.", "fa-duotone fa-taco", 8,
            [new SeedVariant("Slow Beef Barbacoa", "Worth the simmer.", 20, 25, 4, "fa-duotone fa-taco",
                [Spicy, HighProtein],
                [new SeedIngredient("Beef Chuck", 1, "lb"), new SeedIngredient("Corn Tortilla", 8, "whole"), new SeedIngredient("Salsa", 1, "cup")],
                [new SeedInstruction(1, "Simmer the beef until it shreds like a zephyr.")]),
             new SeedVariant("Quick Black Bean", "Pantry to plate.", 5, 10, 4, "fa-duotone fa-taco",
                [Vegan],
                [new SeedIngredient("Black Beans", 1, "can"), new SeedIngredient("Corn Tortilla", 8, "whole")],
                [new SeedInstruction(1, "Warm beans, fill tortillas.")])],
            [new(3.5m), new(4m)]),

        // ---- Dessert (4) ----------------------------------------------------------------------------
        new SeedRecipe("Molten Chocolate Cake", Dessert, null,
            "Individual cakes with a liquid center.", "fa-duotone fa-cake-slice", 2,
            [new SeedVariant("Classic Lava", "Pull them early.", 15, 12, 4, "fa-duotone fa-cake-slice",
                [Vegetarian],
                [new SeedIngredient("Dark Chocolate", 6, "oz"), new SeedIngredient("Butter", 0.5m, "cup"), new SeedIngredient("Eggs", 2, "whole")],
                [new SeedInstruction(1, "Melt chocolate and butter."), new SeedInstruction(2, "Bake just until the edges set.")])],
            [new(5m), new(4.5m), new(5m)]),

        new SeedRecipe("Vegan Banana Bread", Dessert, null,
            "Uses up the sad bananas on your counter.", "fa-duotone fa-bread-loaf", 35,
            [new SeedVariant("One-Bowl Loaf", "No mixer needed.", 15, 40, 8, "fa-duotone fa-bread-loaf",
                [Vegan],
                [new SeedIngredient("Ripe Banana", 3, "whole"), new SeedIngredient("Flour", 2, "cups"), new SeedIngredient("Maple Syrup", 0.5m, "cup")],
                [new SeedInstruction(1, "Mash and mix."), new SeedInstruction(2, "Bake until a pick comes clean.")])],
            [new(4m)]),

        new SeedRecipe("No-Bake Cheesecake", Dessert, null,
            "Chills in the fridge — the oven stays off.", "fa-duotone fa-cheese", 40,
            [new SeedVariant("Graham Crust", "Set and forget.", 20, 0, 8, "fa-duotone fa-cheese",
                [Vegetarian],
                [new SeedIngredient("Cream Cheese", 16, "oz"), new SeedIngredient("Graham Crackers", 1.5m, "cups"), new SeedIngredient("Sugar", 0.5m, "cup")],
                [new SeedInstruction(1, "Press the crust."), new SeedInstruction(2, "Fill and chill overnight.")])],
            [new(3.5m), new(3m)]),

        new SeedRecipe("Matcha Panna Cotta", Dessert, null,
            "Silky, wobbly, faintly grassy.", "fa-duotone fa-pudding", 45,
            [new SeedVariant("Green Tea Set", "Barely any active time.", 15, 10, 4, "fa-duotone fa-pudding",
                [Vegetarian, GlutenFree],
                [new SeedIngredient("Cream", 2, "cups"), new SeedIngredient("Matcha", 1, "tbsp"), new SeedIngredient("Gelatin", 1, "packet")],
                [new SeedInstruction(1, "Warm cream with matcha."), new SeedInstruction(2, "Set with gelatin.")])],
            []),

        // ---- Snack (4) ------------------------------------------------------------------------------
        // Deliberate edge case: a recipe with NO variants -> VariantCount 0 and FastestTime 0.
        new SeedRecipe("Bare Cupboard Snack Board", Snack, null,
            "A placeholder board with no variants yet — edge case for zero-variant indexing.", "fa-duotone fa-plate-utensils", 55,
            [],
            []),

        new SeedRecipe("Crispy Roasted Chickpeas", Snack, null,
            "Addictive, crunchy, high-protein.", "fa-duotone fa-seedling", 60,
            [new SeedVariant("Smoked Paprika", "Shake and roast.", 5, 30, 4, "fa-duotone fa-seedling",
                [Vegan, GlutenFree, HighProtein],
                [new SeedIngredient("Chickpeas", 2, "cans"), new SeedIngredient("Olive Oil", 2, "tbsp"), new SeedIngredient("Smoked Paprika", 1, "tsp")],
                [new SeedInstruction(1, "Dry the chickpeas well."), new SeedInstruction(2, "Roast until crisp.")])],
            [new(4m)]),

        new SeedRecipe("Loaded Nachos", Snack, null,
            "Sharing-optional. Broiler does the work.", "fa-duotone fa-pepper-hot", 70,
            [new SeedVariant("Jalapeno Pile", "Layer for even melt.", 10, 10, 4, "fa-duotone fa-pepper-hot",
                [Vegetarian, Spicy],
                [new SeedIngredient("Tortilla Chips", 1, "bag"), new SeedIngredient("Cheese", 2, "cups"), new SeedIngredient("Jalapeno", 2, "whole")],
                [new SeedInstruction(1, "Layer chips and cheese."), new SeedInstruction(2, "Broil until bubbly.")])],
            [new(3m)]),

        new SeedRecipe("Trail Mix Bark", Snack, null,
            "Melt, scatter, snap.", "fa-duotone fa-cookie", 80,
            [new SeedVariant("Dark Chocolate", "No-cook freezer treat.", 10, 0, 12, "fa-duotone fa-cookie",
                [Vegan, GlutenFree],
                [new SeedIngredient("Dark Chocolate", 8, "oz"), new SeedIngredient("Mixed Nuts", 1, "cup"), new SeedIngredient("Dried Cranberries", 0.5m, "cup")],
                [new SeedInstruction(1, "Melt and spread."), new SeedInstruction(2, "Scatter toppings and freeze.")])],
            [new(2m)]),

        // ---- Beverage (4) — all UNRATED, so filtering to Beverage yields results but no spotlight ----
        // Description-only token "midnight"; author-only token "Salazar" (Diego Salazar).
        new SeedRecipe("Cold Brew Concentrate", Beverage, AuthorDiego,
            "A smooth midnight-dark concentrate to cut with milk or water.", "fa-duotone fa-mug-hot", 30,
            [new SeedVariant("12-Hour Steep", "Steep, strain, keep.", 5, 0, 8, "fa-duotone fa-mug-hot",
                [Vegan, LowCarb],
                [new SeedIngredient("Coarse Coffee", 1, "cup"), new SeedIngredient("Cold Water", 4, "cups")],
                [new SeedInstruction(1, "Steep 12 hours."), new SeedInstruction(2, "Strain.")])],
            []),

        new SeedRecipe("Mango Lassi", Beverage, null,
            "Cooling, sweet, and ready in a blender.", "fa-duotone fa-blender", 90,
            [new SeedVariant("Classic Yogurt", "Blend and pour.", 5, 0, 2, "fa-duotone fa-blender",
                [Vegetarian, GlutenFree],
                [new SeedIngredient("Mango", 1, "cup"), new SeedIngredient("Yogurt", 1, "cup"), new SeedIngredient("Cardamom", null, "pinch", true)],
                [new SeedInstruction(1, "Blend until smooth.")])],
            []),

        new SeedRecipe("Golden Milk", Beverage, null,
            "Turmeric nightcap, gently spiced.", "fa-duotone fa-mug-saucer", 100,
            [new SeedVariant("Coconut Turmeric", "Warm, don't boil.", 5, 5, 2, "fa-duotone fa-mug-saucer",
                [Vegan],
                [new SeedIngredient("Turmeric", 1, "tsp"), new SeedIngredient("Coconut Milk", 2, "cups"), new SeedIngredient("Honey", 1, "tbsp")],
                [new SeedInstruction(1, "Warm everything together.")])],
            []),

        new SeedRecipe("Watermelon Cooler", Beverage, null,
            "Blended summer refresher, no added sugar.", "fa-duotone fa-glass-water", 110,
            [new SeedVariant("Lime & Mint", "Blend, strain, chill.", 10, 0, 4, "fa-duotone fa-glass-water",
                [Vegan, LowCarb],
                [new SeedIngredient("Watermelon", 4, "cups"), new SeedIngredient("Lime", 1, "whole"), new SeedIngredient("Mint", null, "handful", true)],
                [new SeedInstruction(1, "Blend and strain over ice.")])],
            []),
    ];
}
