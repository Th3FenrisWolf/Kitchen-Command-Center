using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace KCC.Contributions.Dashboard;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("kcc/contributions")]
[ApiExplorerSettings(GroupName = "KCC")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
public class ContributionsDashboardController(
    DashboardQueries queries,
    IDashboardMembers members,
    IContributionWrites contributionWrites) : ManagementApiControllerBase
{
    [HttpGet("waiting")]
    [ProducesResponseType<WaitingModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Waiting() => Ok(await queries.WaitingAsync());

    [HttpPost("members/{memberKey:guid}/approval")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid memberKey) =>
        await members.ApproveAsync(memberKey) ? NoContent() : NotFound();

    [HttpGet("reviews")]
    [ProducesResponseType<EntryPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reviews(int page = 0, int pageSize = 20) => Ok(await queries.ReviewsAsync(page, pageSize));

    [HttpPut("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditReview(int id, ReviewEdit edit)
    {
        if (!RatingMath.IsValidRating(edit.Rating))
        {
            return Invalid("A rating runs from 0.5 to 5 in half-star steps.");
        }

        return await contributionWrites.EditReviewAsync(id, edit.Rating, edit.Text) ? NoContent() : NotFound();
    }

    [HttpDelete("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(int id) =>
        await contributionWrites.DeleteReviewByIdAsync(id) ? NoContent() : NotFound();

    [HttpGet("notes")]
    [ProducesResponseType<EntryPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Notes(int page = 0, int pageSize = 20) => Ok(await queries.NotesAsync(page, pageSize));

    [HttpPut("notes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditNote(int id, NoteEdit edit)
    {
        if (string.IsNullOrWhiteSpace(edit.Text))
        {
            return Invalid("A cook note needs text.");
        }

        return await contributionWrites.EditNoteAsync(id, edit.Text) ? NoContent() : NotFound();
    }

    [HttpDelete("notes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(int id) =>
        await contributionWrites.DeleteNoteByIdAsync(id) ? NoContent() : NotFound();

    private BadRequestObjectResult Invalid(string title) => BadRequest(new ProblemDetailsBuilder().WithTitle(title).Build());
}
