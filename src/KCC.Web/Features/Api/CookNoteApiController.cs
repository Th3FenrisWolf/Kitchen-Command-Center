using KCC.Contributions.Data;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api")]
[Authorize]
public class CookNoteApiController(
    IVariantCookNoteInfoProvider variantCookNoteProvider,
    IVariantGuidProvider variantGuidProvider,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    [HttpGet("variant/{variantGuid:guid}/notes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNotes(
        Guid variantGuid,
        [FromServices] AuthorNameProvider authorNameProvider,
        int page = 0,
        int pageSize = 10)
    {
        var rows = variantCookNoteProvider.GetForVariant(variantGuid, page, pageSize, out var total);
        var authorNames = await authorNameProvider.ResolveMany(rows.Select(r => r.MemberGuid));
        var memberGuid = await CurrentMemberGuidOrEmpty();

        var notes = rows.Select(r => new
        {
            id = r.VariantCookNoteID,
            authorName = authorNames.GetValueOrDefault(r.MemberGuid) ?? "(deleted)",
            text = r.NoteText,
            created = r.NoteCreated,
            isMine = memberGuid != Guid.Empty && r.MemberGuid == memberGuid,
        });

        return Ok(new { total, page, pageSize, notes });
    }

    [HttpPost("variant/{variantGuid:guid}/note")]
    public async Task<IActionResult> AddNote(Guid variantGuid, [FromBody] string request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return BadRequest(new { error = "Note text is required." });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var recipeGuid = await variantGuidProvider.GetRecipeGuidAsync(variantGuid, cancellationToken);
        if (recipeGuid is null)
        {
            return NotFound(new { error = "Variant not found." });
        }

        var id = variantCookNoteProvider.Add(variantGuid, recipeGuid.Value, user.MemberGuid, request);
        return Ok(new { id });
    }

    [HttpDelete("note/{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deleted = variantCookNoteProvider.DeleteOwn(id, user.MemberGuid);
        return deleted
            ? Ok(new { success = true })
            : StatusCode(403, new { error = "You can only delete your own note." });
    }

    private async Task<Guid> CurrentMemberGuidOrEmpty()
    {
        if (User?.Identity?.IsAuthenticated is not true)
        {
            return Guid.Empty;
        }

        var user = await userManager.GetUserAsync(User);
        return user?.MemberGuid ?? Guid.Empty;
    }
}
