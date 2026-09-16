using KCC.Web.Features.Extensions;
using KCC.Web.Features.Sections.Base;

namespace KCC.UnitTests.Features.Sections;

public class SectionColorsTests
{
    // The ink contrast.test.ts pins for each ground. Spelling the pairs out by hand keeps this independent
    // of how SectionColors derives its wash set, so a change to that derivation has to break a test.
    private static readonly Dictionary<SectionBackgroundColorOptions, string> ExpectedInk = new()
    {
        [SectionBackgroundColorOptions.Desk] = "text-ink",
        [SectionBackgroundColorOptions.Paper] = "text-ink",
        [SectionBackgroundColorOptions.PaperTwo] = "text-ink",
        [SectionBackgroundColorOptions.Peach] = "text-ink-on-wash",
        [SectionBackgroundColorOptions.Yellow] = "text-ink-on-wash",
        [SectionBackgroundColorOptions.Green] = "text-ink-on-wash",
        [SectionBackgroundColorOptions.Teal] = "text-ink-on-wash",
        [SectionBackgroundColorOptions.Sky] = "text-ink-on-wash",
        [SectionBackgroundColorOptions.Lavender] = "text-ink-on-wash",
    };

    [Test]
    public async Task TextClassFor_GivesEveryOptionTheInkPinnedForItsGround()
    {
        var options = Enum.GetValues<SectionBackgroundColorOptions>();

        // An option with no row above is a ground whose ink nobody has contrast-checked.
        _ = await Assert.That(ExpectedInk.Count).IsEqualTo(options.Length);

        foreach (var option in options)
        {
            _ = await Assert.That(SectionColors.TextClassFor(option.GetTailwindStyle()))
                .IsEqualTo(ExpectedInk[option]);
        }
    }

    [Test]
    [Arguments("bg-bone")]
    [Arguments("bg-surface-500")]
    [Arguments("bg-desk-2")]
    [Arguments("")]
    [Arguments((string)null)]
    public async Task TextClassFor_FallsBackToInkForGroundsItDoesNotRecognise(string backgroundClass)
    {
        // Retired tokens still sit in published content and paint nothing, so the desk shows through.
        // Assuming a wash there would put near-black ink on the darkest ground in the ramp.
        _ = await Assert.That(SectionColors.TextClassFor(backgroundClass)).IsEqualTo("text-ink");
    }
}
