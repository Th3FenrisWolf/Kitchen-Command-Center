using KCC.Contributions;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api")]
public class CookNoteApiController(
    IContributionReads contributionReads,
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
}
