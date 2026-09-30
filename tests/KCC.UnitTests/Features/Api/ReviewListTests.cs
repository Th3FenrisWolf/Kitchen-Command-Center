using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Moq;
using Umbraco.Cms.Core.Security;

namespace KCC.UnitTests.Features.Api;

public class ReviewListTests
{
    private static readonly Guid VariantKey = Guid.NewGuid();

    [Test]
    public async Task GetReviews_NamesReviewersAndMarksDeletedOnes()
    {
        var priya = Guid.NewGuid();
        var controller = Controller([Review(priya, 5m), Review(Guid.NewGuid(), 4m)], new() { [priya] = "Priya Balan" });

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(string.Join(",", response.Reviews.Select(review => review.AuthorName))).IsEqualTo("Priya Balan,(deleted)");
    }

    [Test]
    public async Task GetReviews_SignedOut_HasNoOwnReviewAndMarksNothingMine()
    {
        var controller = Controller([Review(Guid.NewGuid(), 5m)], []);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.MyReview).IsNull();
        _ = await Assert.That(response.Reviews.Any(review => review.IsMine)).IsFalse();
    }

    [Test]
    public async Task GetReviews_SignedIn_ReturnsAndMarksTheMembersReview()
    {
        var member = Guid.NewGuid();
        var mine = Review(member, 4.5m, "Crispy edges");
        var controller = Controller([mine, Review(Guid.NewGuid(), 3m)], [], member, mine);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.MyReview).IsEqualTo(new MyReview(4.5m, "Crispy edges"));
        _ = await Assert.That(string.Join(",", response.Reviews.Select(review => review.IsMine))).IsEqualTo("True,False");
    }

    [Test]
    public async Task GetReviews_ReportsTheVariantsAggregateAndDistribution()
    {
        var controller = Controller([Review(Guid.NewGuid(), 5m), Review(Guid.NewGuid(), 4m)], []);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.Average).IsEqualTo(4.5d);
        _ = await Assert.That(response.Count).IsEqualTo(2);
        _ = await Assert.That(string.Join(",", response.Distribution)).IsEqualTo("0,0,0,1,1");
    }

    [Test]
    public async Task GetReviews_ReportsThePageItServed()
    {
        var controller = Controller([], []);

        var response = (await controller.GetReviews(VariantKey, page: -3, pageSize: 500)).Value;

        _ = await Assert.That(response.Page).IsEqualTo(0);
        _ = await Assert.That(response.PageSize).IsEqualTo(ContributionReads.MaxPageSize);
    }

    private static Review Review(Guid memberKey, decimal rating, string text = null) => new()
    {
        VariantKey = VariantKey,
        MemberKey = memberKey,
        Rating = rating,
        Text = text,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static ReviewApiController Controller(Review[] reviews, Dictionary<Guid, string> names, Guid? memberKey = null, Review mine = null)
    {
        var stats = new Mock<IContributionStats>();
        stats.Setup(s => s.GetAsync()).ReturnsAsync(ContributionStats.Build(reviews.Select(review => (review.VariantKey, review.Rating)), []));

        var reads = new Mock<IContributionReads>();
        reads.Setup(r => r.ReviewsAsync(VariantKey, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new Paged<Review>(reviews, reviews.Length));
        reads.Setup(r => r.MemberReviewAsync(VariantKey, It.IsAny<Guid>())).ReturnsAsync(mine);

        var authors = new Mock<IAuthorNameProvider>();
        authors.Setup(a => a.ResolveMany(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(names);

        var members = new Mock<IMemberManager>();
        members.Setup(m => m.GetCurrentMemberAsync())
            .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);

        return new ReviewApiController(stats.Object, reads.Object, Mock.Of<IContributionWrites>(), Mock.Of<IRecipeQueries>(), authors.Object, members.Object);
    }
}
