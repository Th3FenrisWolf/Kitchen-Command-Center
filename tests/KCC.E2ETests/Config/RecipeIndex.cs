using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class RecipeIndex
{
    // Serial tests publish recipes and write ratings, and the index rebuilds after a quiet period, so a later test
    // could watch the library's first page change mid-read. Seeding a seeded site writes nothing and answers only
    // once the index holds every earlier write.
    public static async Task CurrentAsync(IPage page)
    {
        var response = await page.APIRequest.PostAsync("/api/dev/seed-recipes", new() { Timeout = 120_000 });
        if (!response.Ok)
        {
            throw new InvalidOperationException($"Waiting for the recipe index answered {response.Status}.");
        }
    }
}
