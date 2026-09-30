using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.RecipeSeed;

[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevSeedApiController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromServices] RecipeTestDataSeeder seeder, CancellationToken cancellationToken)
    {
        // The integration and E2E fixtures run the site in the Testing environment.
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        using var log = new StringWriter();
        var summary = await seeder.RunAsync(log, cancellationToken);
        return Ok(new { summary = summary.ToString(), log = log.ToString() });
    }
}
