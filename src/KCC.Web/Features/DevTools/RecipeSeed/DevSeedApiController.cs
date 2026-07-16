using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.RecipeSeed;

/// <summary>
/// Development/CI-only endpoint that runs the recipe search <see cref="RecipeTestDataSeeder"/>. Exposed as an
/// HTTP endpoint (rather than a CLI command) so it executes inside a real request: Kentico's Lucene
/// indexing worker only drains its queue in that context, so the reindex the seeder triggers actually
/// completes before the response returns. It is also enabled under the CI environment so the E2E
/// pipeline can populate recipes and build the search index. Returns 404 in all other environments.
/// </summary>
[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevSeedApiController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Seeds the recipe search test-data set and rebuilds the index. Idempotent.</summary>
    /// <param name="reset">When true, delete previously-seeded recipes and their reviews first, then re-create them cleanly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary of what was created (or 404 outside Development/CI).</returns>
    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromQuery] bool reset, CancellationToken cancellationToken)
    {
        // Enabled in Development and in CI (the E2E pipeline runs with ASPNETCORE_ENVIRONMENT=CI).
        if (!environment.IsDevelopment() && !environment.IsEnvironment("CI"))
        {
            return NotFound();
        }

        using var log = new StringWriter();
        var summary = await RecipeTestDataSeeder.RunAsync(HttpContext.RequestServices, log, reset, cancellationToken);
        return Ok(new { summary = summary.ToString(), log = log.ToString() });
    }
}
