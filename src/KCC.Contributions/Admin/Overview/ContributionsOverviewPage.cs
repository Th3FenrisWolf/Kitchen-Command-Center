using CMS.Membership;
using KCC.Contributions.Admin;
using KCC.Contributions.Admin.Overview;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(ContributionsApplication),
    slug: "overview",
    uiPageType: typeof(ContributionsOverviewPage),
    name: "Overview",
    templateName: "@kcc/contributions/Overview",
    order: 0
)]

namespace KCC.Contributions.Admin.Overview;

public class ContributionsOverviewPage(ContributionsOverviewService overviewService)
    : Page<ContributionsOverviewPageProperties>
{
    public override Task<ContributionsOverviewPageProperties> ConfigureTemplateProperties(ContributionsOverviewPageProperties properties)
    {
        properties.RecipePageSize = ContributionsOverviewService.RecipePageSize;
        properties.EntryPageSize = ContributionsOverviewService.EntryPageSize;
        return Task.FromResult(properties);
    }

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<RecipeOverviewPageResult>> GetRecipes(RecipeOverviewQueryArgs args) =>
        ResponseFrom(await overviewService.GetRecipesAsync(args, CancellationToken.None));

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<RecipeVariantsResult>> GetRecipeVariants(RecipeVariantsArgs args) =>
        Task.FromResult(ResponseFrom(overviewService.GetRecipeVariants(args.RecipeGuid)));

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<ReviewEntriesResult>> GetVariantReviews(VariantEntriesArgs args) =>
        Task.FromResult(ResponseFrom(overviewService.GetVariantReviews(args)));

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<CookNoteEntriesResult>> GetVariantCookNotes(VariantEntriesArgs args) =>
        Task.FromResult(ResponseFrom(overviewService.GetVariantCookNotes(args)));

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public Task<ICommandResponse<DeleteEntryResult>> DeleteReview(DeleteEntryArgs args) =>
        Task.FromResult(DeleteResponse(overviewService.DeleteReview(args.Id), "Review deleted.", "Review not found."));

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public Task<ICommandResponse<DeleteEntryResult>> DeleteCookNote(DeleteEntryArgs args) =>
        Task.FromResult(DeleteResponse(overviewService.DeleteCookNote(args.Id), "Cook note deleted.", "Cook note not found."));

    private ICommandResponse<DeleteEntryResult> DeleteResponse(bool deleted, string successMessage, string errorMessage)
    {
        var response = ResponseFrom(new DeleteEntryResult(deleted));
        return deleted ? response.AddSuccessMessage(successMessage) : response.AddErrorMessage(errorMessage);
    }
}
