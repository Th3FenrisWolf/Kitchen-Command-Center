using KCC.Web.Features.Sections.Base;

namespace KCC.UnitTests.Features.Sections;

public class SpacingLadderTests
{
    // Margins between blocks are multiples of 24px (torn-and-waxed.md → Layout rhythm); Tailwind's unit
    // is 4px, so every step of the ladder is a multiple of six units.
    private const int UnitsPerRule = 6;

    [Test]
    public async Task GetSpacing_KeepsEveryStepOnTheRule()
    {
        foreach (var spacing in Enum.GetValues<Spacing>())
        {
            _ = await Assert.That(BaseSectionProperties.GetSpacing(spacing) % UnitsPerRule).IsEqualTo(0);
        }
    }

    [Test]
    public async Task GetSpacing_GrowsFromNoneToLarge()
    {
        var ladder = Enum.GetValues<Spacing>().Select(BaseSectionProperties.GetSpacing).ToArray();

        _ = await Assert.That(ladder[0]).IsEqualTo(0);
        _ = await Assert.That(ladder).IsEquivalentTo(ladder.Order().ToArray());
        _ = await Assert.That(ladder.Distinct().Count()).IsEqualTo(ladder.Length);
    }
}
