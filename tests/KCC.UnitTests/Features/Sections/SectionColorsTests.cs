using KCC.Web.Features.Extensions;
using KCC.Web.Features.Sections.Base;

namespace KCC.UnitTests.Features.Sections;

public class SectionColorsTests
{
    // A section is a ground; washes are pools under sheets. These four are the grounds contrast.test.ts
    // pins ink against in both ramps, so an option outside the list is one nobody has contrast-checked.
    private static readonly SectionBackgroundColorOptions[] ContrastCheckedGrounds =
    [
        SectionBackgroundColorOptions.Desk,
        SectionBackgroundColorOptions.DeskTwo,
        SectionBackgroundColorOptions.Paper,
        SectionBackgroundColorOptions.PaperTwo,
    ];

    [Test]
    public async Task BackgroundOptions_OfferGroundsOnly()
    {
        _ = await Assert.That(Enum.GetValues<SectionBackgroundColorOptions>())
            .IsEquivalentTo(ContrastCheckedGrounds);
    }

    [Test]
    public async Task TextClassFor_GivesEveryGroundInk()
    {
        foreach (var option in Enum.GetValues<SectionBackgroundColorOptions>())
        {
            _ = await Assert.That(SectionColors.TextClassFor(option.GetTailwindStyle()))
                .IsEqualTo("text-ink");
        }
    }

    [Test]
    [Arguments("bg-peach")]
    [Arguments("bg-bone")]
    [Arguments("bg-surface-500")]
    [Arguments("")]
    [Arguments((string)null)]
    public async Task TextClassFor_GivesInkToBackgroundsItDoesNotRecognise(string backgroundClass)
    {
        // Published content still holds the washes sections used to offer and classes that paint nothing at
        // all. Ink reads on every one of them, which is why nothing here branches.
        _ = await Assert.That(SectionColors.TextClassFor(backgroundClass)).IsEqualTo("text-ink");
    }
}
