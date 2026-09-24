using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.Baseline;

[ApiController]
[Route("api/dev/baseline")]
public class BaselineApiController(IWebHostEnvironment environment, BaselineExport baselineExport) : ControllerBase
{
    [HttpPost("export")]
    public async Task<IActionResult> Export()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await baselineExport.RunAsync();
        return result.Succeeded ? Ok(result.Message) : Conflict(result.Message);
    }
}
