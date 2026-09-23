using CMS.Helpers;
using KCC.Contributions.Admin;

namespace KCC.UnitTests.Features.Contributions;

public class ContributionFormItemsTests
{
    [Test]
    public async Task NormalizeText_ReturnsNullForBlankInput()
    {
        _ = await Assert.That(ContributionFormItems.NormalizeText(null)).IsNull();
        _ = await Assert.That(ContributionFormItems.NormalizeText("")).IsNull();
        _ = await Assert.That(ContributionFormItems.NormalizeText("   ")).IsNull();
    }

    [Test]
    public async Task NormalizeText_TrimsContent()
    {
        _ = await Assert.That(ContributionFormItems.NormalizeText("  tasty  ")).IsEqualTo("tasty");
    }

    [Test]
    public async Task FormatUtc_ReturnsEmptyForZeroTime()
    {
        _ = await Assert.That(ContributionFormItems.FormatUtc(DateTimeHelper.ZERO_TIME)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FormatUtc_FormatsTimestamp()
    {
        var value = new DateTime(2026, 8, 12, 15, 30, 0, DateTimeKind.Utc);
        _ = await Assert.That(ContributionFormItems.FormatUtc(value)).IsEqualTo("2026-08-12 15:30 UTC");
    }

    [Test]
    public async Task ReviewTitle_ComposesVariantAndMember()
    {
        var variantGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [variantGuid] = "12-Hour Steep" };
        var members = new Dictionary<Guid, MemberDisplay> { [memberGuid] = new("Ana Cook", "ana@example.com") };

        var title = ContributionTitles.Review(names, members, variantGuid, memberGuid);

        _ = await Assert.That(title).IsEqualTo("Review of 12-Hour Steep — Ana Cook");
    }

    [Test]
    public async Task NoteTitle_FallsBackToDeletedForOrphans()
    {
        var title = ContributionTitles.Note(
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, MemberDisplay>(),
            Guid.NewGuid(),
            Guid.NewGuid());

        _ = await Assert.That(title).IsEqualTo("Note on (deleted) — (deleted)");
    }
}
