using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Moq;
using Umbraco.Cms.Core.Security;

namespace KCC.UnitTests.Features.Api;

public class CookNoteListTests
{
    private static readonly Guid VariantKey = Guid.NewGuid();

    [Test]
    public async Task GetNotes_NamesAuthorsAndMarksTheMembersOwn()
    {
        var member = Guid.NewGuid();
        var controller = Controller([Note(1, member, "Use buttermilk"), Note(2, Guid.NewGuid(), "Rest the batter")], new() { [member] = "Priya Balan" }, member);

        var response = (await controller.GetNotes(VariantKey)).Value;

        _ = await Assert.That(string.Join(",", response.Notes.Select(note => note.AuthorName))).IsEqualTo("Priya Balan,(deleted)");
        _ = await Assert.That(string.Join(",", response.Notes.Select(note => note.IsMine))).IsEqualTo("True,False");
        _ = await Assert.That(response.Notes.First().Id).IsEqualTo(1);
    }

    [Test]
    public async Task GetNotes_ReportsTheTotalAndThePageItServed()
    {
        var controller = Controller([Note(1, Guid.NewGuid(), "Rest the batter")], []);

        var response = (await controller.GetNotes(VariantKey, page: 2, pageSize: 0)).Value;

        _ = await Assert.That(response.Total).IsEqualTo(1);
        _ = await Assert.That(response.Page).IsEqualTo(2);
        _ = await Assert.That(response.PageSize).IsEqualTo(1);
    }

    private static CookNote Note(int id, Guid memberKey, string text) => new()
    {
        Id = id,
        VariantKey = VariantKey,
        MemberKey = memberKey,
        Text = text,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static CookNoteApiController Controller(CookNote[] notes, Dictionary<Guid, string> names, Guid? memberKey = null)
    {
        var reads = new Mock<IContributionReads>();
        reads.Setup(r => r.NotesAsync(VariantKey, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new Paged<CookNote>(notes, notes.Length));

        var authors = new Mock<IAuthorNameProvider>();
        authors.Setup(a => a.ResolveMany(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(names);

        var members = new Mock<IMemberManager>();
        members.Setup(m => m.GetCurrentMemberAsync())
            .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);

        return new CookNoteApiController(reads.Object, Mock.Of<IContributionWrites>(), Mock.Of<IRecipeQueries>(), authors.Object, members.Object);
    }
}
