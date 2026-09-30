using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KCC.Web.Features.Pages.Home;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.Pages.Home;

public class SectionStyleTests
{
    private static readonly string[] Backgrounds = ["Desk", "Desk two", "Paper", "Paper two"];

    private static readonly string[] Widths = ["Thin", "Container", "Breakout", "Full width"];

    private static readonly string[] Washes = ["Peach", "Yellow", "Green", "Teal", "Sky", "Lavender", "Pink", "Red"];

    [Test]
    public async Task Classes_Unset_LeaveTheSectionOnTheDeskAtContainerWidth()
    {
        _ = await Assert.That(SectionStyle.Classes(null, null)).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("Desk", "")]
    [Arguments("Desk two", "bg-desk-2 p-6 lg:p-12")]
    [Arguments("Paper", "bg-paper p-6 lg:p-12")]
    [Arguments("Paper two", "bg-paper-2 p-6 lg:p-12")]
    public async Task Classes_Background_FillsAndPadsEveryGroundButTheDesk(string background, string expected)
    {
        _ = await Assert.That(SectionStyle.Classes(background, null)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("Thin", "thin")]
    [Arguments("Container", "")]
    [Arguments("Breakout", "breakout")]
    [Arguments("Full width", "full-width px-4")]
    public async Task Classes_Width_ChoosesTheContentGridColumn(string width, string expected)
    {
        _ = await Assert.That(SectionStyle.Classes(null, width)).IsEqualTo(expected);
    }

    [Test]
    public async Task Classes_PutTheWidthBeforeTheFill()
    {
        _ = await Assert.That(SectionStyle.Classes("Paper", "Breakout")).IsEqualTo("breakout bg-paper p-6 lg:p-12");
    }

    [Test]
    public async Task Classes_FullWidthFill_PadsInsteadOfTakingAGutter()
    {
        _ = await Assert.That(SectionStyle.Classes("Paper two", "Full width")).IsEqualTo("full-width bg-paper-2 p-6 lg:p-12");
    }

    [Test]
    [Arguments("Peach", "bg-peach")]
    [Arguments("Lavender", "bg-lavender")]
    [Arguments("", "")]
    [Arguments(null, "")]
    public async Task Wash_IsTheFillUtility(string wash, string expected)
    {
        _ = await Assert.That(SectionStyle.Wash(wash)).IsEqualTo(expected);
    }

    // Tailwind never scans .cs files, so a class built here that the safelist misses renders unstyled, silently.
    [Test]
    public async Task EveryClassItCanBuild_IsInTheTailwindSafelist()
    {
        var safelist = Safelist();
        var built = Backgrounds.SelectMany(background => Widths.Select(width => SectionStyle.Classes(background, width)))
            .Concat(Washes.Select(SectionStyle.Wash))
            .SelectMany(classes => classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Except(["thin", "breakout", "full-width"])
            .Distinct();

        _ = await Assert.That(built.Where(name => !safelist.Contains(name))).IsEmpty();
    }

    // The backoffice stores each option's text as the content's value and SectionStyle matches it by string, so an
    // option renamed or added there renders unstyled, silently.
    [Test]
    public async Task SectionBackgroundOptions_MatchTheBackgroundsTheStyleHandles()
    {
        _ = await Assert.That(DropdownItems("KCCSectionBackground")).IsEquivalentTo(Backgrounds, CollectionOrdering.Matching);
    }

    [Test]
    public async Task SectionWidthOptions_MatchTheWidthsTheStyleHandles()
    {
        _ = await Assert.That(DropdownItems("KCCSectionWidth")).IsEquivalentTo(Widths, CollectionOrdering.Matching);
    }

    [Test]
    public async Task WashOptions_MatchTheWashesTheStyleHandles()
    {
        _ = await Assert.That(DropdownItems("KCCWash")).IsEquivalentTo(Washes, CollectionOrdering.Matching);
    }

    [Test]
    public async Task SectionBackgroundOptions_OnlyTheDeskLeavesTheSectionUnfilled()
    {
        var unfilled = DropdownItems("KCCSectionBackground").Where(background => SectionStyle.Classes(background, null).Length == 0);

        _ = await Assert.That(unfilled).IsEquivalentTo(["Desk"]);
    }

    [Test]
    public async Task SectionWidthOptions_OnlyTheContainerKeepsTheDefaultColumn()
    {
        var defaultColumn = DropdownItems("KCCSectionWidth").Where(width => SectionStyle.Classes(null, width).Length == 0);

        _ = await Assert.That(defaultColumn).IsEquivalentTo(["Container"]);
    }

    [Test]
    public async Task WashOptions_EveryOneFills()
    {
        var unfilled = DropdownItems("KCCWash").Where(wash => SectionStyle.Wash(wash).Length == 0);

        _ = await Assert.That(unfilled).IsEmpty();
    }

    private static string[] DropdownItems(string dataType)
    {
        var path = Path.Combine(RepositoryRoot(), "src", "KCC.Web", "uSync", "v17", "DataTypes", $"{dataType}.config");
        var config = XDocument.Load(path).Root.Element("Config").Value;
        return JsonNode.Parse(config)["items"].AsArray().Select(item => (string)item).ToArray();
    }

    private static HashSet<string> Safelist()
    {
        var css = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "KCC.Web", "Features", "Styles", "TailwindConfig.css"));
        return Regex.Matches(css, @"@source inline\('([^']+)'\)")
            .SelectMany(match => Expand(match.Groups[1].Value))
            .ToHashSet();
    }

    private static IEnumerable<string> Expand(string pattern)
    {
        var group = Regex.Match(pattern, @"\{([^}]*)\}");
        return group.Success
            ? group.Groups[1].Value.Split(',').SelectMany(option =>
                Expand(pattern[..group.Index] + option + pattern[(group.Index + group.Length)..]))
            : [pattern];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
