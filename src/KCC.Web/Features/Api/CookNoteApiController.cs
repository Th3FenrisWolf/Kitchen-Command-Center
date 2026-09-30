using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api")]
[AutoValidateAntiforgeryToken]
public class CookNoteApiController(
    IContributionReads contributionReads,
    IContributionWrites contributionWrites,
    IRecipeQueries recipes,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("variant/{variantGuid:guid}/notes")]
    public async Task<ActionResult<CookNotesResponse>> GetNotes(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var notes = await contributionReads.NotesAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(notes.Items.Select(note => note.MemberKey));
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;

        return new CookNotesResponse(
            notes.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            notes.Items.Select(note => new CookNoteItem(
                note.Id,
                names.GetValueOrDefault(note.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                note.Text,
                note.Created,
                note.MemberKey == memberKey)).ToList());
    }

    [HttpPost("variant/{variantGuid:guid}/note")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> AddNote(Guid variantGuid, [FromBody] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { error = "Note text is required." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        return Ok(new { id = await contributionWrites.AddNoteAsync(variantGuid, member.Key, text) });
    }

    [HttpDelete("note/{id:int}")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> DeleteNote(int id)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await contributionWrites.DeleteOwnNoteAsync(id, member.Key)
            ? Ok(new { success = true })
            : StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only delete your own note." });
    }
}
