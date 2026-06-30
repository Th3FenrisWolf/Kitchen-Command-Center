using KCC.Contributions.Data;
using KCC.Web.Features.Members;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>Read/write API for community cook notes on a variant.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class CookNoteApiController(
    IVariantCookNoteInfoProvider notes,
    IVariantGuidResolver variantGuidResolver,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    /// <summary>Add-note request body.</summary>
    /// <param name="Text">The note text.</param>
    public record NoteRequest(string Text);

    /// <summary>Returns a page of notes for the variant with author names.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="authorNameResolver">Resolves member display names for attribution.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A page of notes with author names.</returns>
    [HttpGet("variant/{variantGuid:guid}/notes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNotes(
        Guid variantGuid,
        [FromServices] IAuthorNameResolver authorNameResolver,
        int page = 0,
        int pageSize = 10)
    {
        var rows = notes.GetForVariant(variantGuid, page, pageSize, out var total);
        var authorNames = await authorNameResolver.ResolveMany(rows.Select(r => r.MemberGuid));
        var memberGuid = await CurrentMemberGuidOrEmpty();

        var dtos = rows.Select(r => new
        {
            id = r.VariantCookNoteID,
            authorName = authorNames.GetValueOrDefault(r.MemberGuid) ?? "(deleted)",
            text = r.NoteText,
            created = r.NoteCreated,
            isMine = memberGuid != Guid.Empty && r.MemberGuid == memberGuid,
        });

        return Ok(new { total, page, pageSize, notes = dtos });
    }

    /// <summary>Adds a cook note authored by the current member.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="request">The note text to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ok with the new note id; BadRequest for empty text; NotFound for an unknown variant; Unauthorized when not signed in.</returns>
    [HttpPost("variant/{variantGuid:guid}/note")]
    public async Task<IActionResult> AddNote(Guid variantGuid, [FromBody] NoteRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Note text is required." });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var recipeGuid = await variantGuidResolver.ResolveRecipeGuidAsync(variantGuid, cancellationToken);
        if (recipeGuid is null)
        {
            return NotFound(new { error = "Variant not found." });
        }

        var id = notes.Add(variantGuid, recipeGuid.Value, user.MemberGuid, request.Text);
        return Ok(new { id });
    }

    /// <summary>Deletes a cook note; author only.</summary>
    /// <param name="id">The cook-note id.</param>
    /// <returns>Ok when removed; 403 when the note is not the current member's (or missing); Unauthorized when not signed in.</returns>
    [HttpDelete("note/{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deleted = notes.DeleteOwn(id, user.MemberGuid);
        return deleted
            ? Ok(new { success = true })
            : StatusCode(403, new { error = "You can only delete your own note." });
    }

    private async Task<Guid> CurrentMemberGuidOrEmpty()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Guid.Empty;
        }

        var user = await userManager.GetUserAsync(User);
        return user?.MemberGuid ?? Guid.Empty;
    }
}
