using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace KCC.Admin;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("kcc/recipe-editor")]
[ApiExplorerSettings(GroupName = "KCC")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public class RecipeEditorController(IRecipeIconService iconService) : ManagementApiControllerBase
{
    [HttpGet("icons")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IActionResult Icons() => Ok(RecipeIcons.All);

    [HttpGet("units")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IActionResult Units() => Ok(RecipeUnits.All);

    [HttpPost("icon-suggestion")]
    [ProducesResponseType<IconSuggestion>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestIcon(IconSuggestionRequest request, CancellationToken cancellationToken)
    {
        var icon = await iconService.PickAsync(request.Name ?? string.Empty, request.Description ?? string.Empty, [], cancellationToken);
        return Ok(new IconSuggestion(icon));
    }
}

public sealed record IconSuggestionRequest(string Name, string Description);

public sealed record IconSuggestion(string Icon);
