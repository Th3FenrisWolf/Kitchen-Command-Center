using KCC.IntegrationTests.Config;
using KCC.Web.Features.Pages.Account;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Pages;

public class KitchenSummariesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ASubmission_ShowsInTheMembersSummary_OnTheNextRead()
    {
        var member = await TestMembers.ApprovedAsync(Site.Services, TestMembers.UniqueUserName("summary"));

        var before = SummaryOf(member);
        await TestContent.DraftRecipeAsync(Site.Services, "IT Gannet", member);
        var after = SummaryOf(member);

        _ = await Assert.That(before).IsEqualTo(new KitchenSummary(0, 0, 0));
        _ = await Assert.That(after).IsEqualTo(new KitchenSummary(1, 0, 1));
    }

    private KitchenSummary? SummaryOf(Guid member)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        return Site.Services.GetRequiredService<KitchenSummaries>().For(member);
    }
}
