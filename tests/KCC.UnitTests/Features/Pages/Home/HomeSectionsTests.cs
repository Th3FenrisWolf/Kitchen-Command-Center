using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Home;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;

namespace KCC.UnitTests.Features.Pages.Home;

public class HomeSectionsTests
{
    [Test]
    public async Task From_KeepsTheEditorsOrder()
    {
        var sections = HomeSections.From(Blocks(RichText("<p>One</p>"), Grid(null, "2", Card("Beef")), Stacker("Drinks", Card("Gin"))));

        _ = await Assert.That(string.Join(",", sections.Select(section => section.Alias))).IsEqualTo("richTextBlock,cardGridBlock,stackerBlock");
    }

    [Test]
    public async Task From_WithoutSections_IsEmpty()
    {
        _ = await Assert.That(HomeSections.From(null)).IsEmpty();
    }

    [Test]
    public async Task From_StylesEachSectionFromItsSettings()
    {
        var section = HomeSections.From(Blocks((RichText("<p>One</p>"), Settings("Paper", "Breakout")))).Single();

        _ = await Assert.That(section.Classes).IsEqualTo("breakout bg-paper p-6 lg:p-12");
    }

    [Test]
    public async Task From_RunsOneTearCycleDownThePage()
    {
        var sections = HomeSections.From(Blocks(
            RichText("<p>One</p>"),
            Grid(null, "2", Card("Beef"), Card("Chicken")),
            Stacker("Drinks", "<p>Pour.</p>", Card("Gin"), Card("Rum")),
            RichText("<p>Two</p>")));
        var grid = (CardGridSection)sections[1];
        var stacker = (StackerSection)sections[2];

        _ = await Assert.That(((RichTextSection)sections[0]).Tear).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", grid.Cards.Select(card => card.Tear))).IsEqualTo("2,3");
        _ = await Assert.That(stacker.Tear).IsEqualTo(4);
        _ = await Assert.That(string.Join(",", stacker.Cards.Select(card => card.Tear))).IsEqualTo("5,6");
        _ = await Assert.That(((RichTextSection)sections[3]).Tear).IsEqualTo(1);
    }

    [Test]
    public async Task From_StackerCards_SkipTheSheetsTear()
    {
        var stacker = (StackerSection)HomeSections.From(Blocks(
            Stacker("Drinks", "<p>Pour.</p>", Card("Vodka"), Card("Tequila"), Card("Gin"), Card("Whiskey"), Card("Rum"), Card("Wine")))).Single();

        _ = await Assert.That(stacker.Tear).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", stacker.Cards.Select(card => card.Tear))).IsEqualTo("2,3,4,5,6,2");
    }

    [Test]
    public async Task From_FirstTwoCardsBelowAStacker_SkipItsSheetsTear()
    {
        var sections = HomeSections.From(Blocks(
            Stacker("Drinks", "<p>Pour.</p>", Card("Gin"), Card("Rum"), Card("Port"), Card("Wine")),
            Grid(null, "4", Card("Beef"), Card("Chicken"), Card("Pork"))));
        var grid = (CardGridSection)sections[1];

        _ = await Assert.That(string.Join(",", grid.Cards.Select(card => card.Tear))).IsEqualTo("6,2,3");
    }

    [Test]
    public async Task From_RichText_CarriesItsMarkup()
    {
        var section = (RichTextSection)HomeSections.From(Blocks(RichText("<h1>Welcome</h1>"))).Single();

        _ = await Assert.That(section.Html).IsEqualTo("<h1>Welcome</h1>");
    }

    [Test]
    [Arguments("3", 3)]
    [Arguments("4", 4)]
    [Arguments(null, 2)]
    [Arguments("9", 2)]
    public async Task From_CardGrid_ReadsItsColumns(string columns, int expected)
    {
        var grid = (CardGridSection)HomeSections.From(Blocks(Grid(null, columns, Card("Beef")))).Single();

        _ = await Assert.That(grid.Columns).IsEqualTo(expected);
    }

    [Test]
    public async Task From_CardGrid_MapsEachCard()
    {
        var link = new Link { Name = "View Recipes", Url = "/recipes/", Target = "_blank" };

        var grid = (CardGridSection)HomeSections.From(Blocks(Grid("Sweets", "4", Card("Cake", "Rich", "Layers.", "Red", link)))).Single();

        _ = await Assert.That(grid.Cards.Single()).IsEqualTo(new HomeCard("Cake", "Rich", "Layers.", "bg-red", new HomeLink("View Recipes", "/recipes/", "_blank"), 1));
    }

    [Test]
    public async Task From_CardLinkThatResolvesNowhere_IsDropped()
    {
        var grid = (CardGridSection)HomeSections.From(Blocks(Grid(null, "2", Card("Cake", link: new Link { Name = "Gone" })))).Single();

        _ = await Assert.That(grid.Cards.Single().Link).IsNull();
    }

    [Test]
    public async Task From_Stacker_GivesTheComponentItsCards()
    {
        var stacker = (StackerSection)HomeSections.From(Blocks(Stacker("Drinks", Card("Gin", "Botanical", wash: "Green")))).Single();

        _ = await Assert.That(stacker.Heading).IsEqualTo("Drinks");
        _ = await Assert.That(stacker.HasSheet).IsFalse();
        _ = await Assert.That(stacker.Cards.Single()).IsEqualTo(new StackerCard("Gin", "Botanical", "bg-green", 1));
    }

    private static BlockListModel Blocks(params IPublishedElement[] contents) =>
        Blocks(contents.Select(content => (content, (IPublishedElement)null)).ToArray());

    private static BlockListModel Blocks(params (IPublishedElement Content, IPublishedElement Settings)[] blocks) =>
        new(blocks.Select(block => new BlockListItem(Guid.NewGuid(), block.Content, block.Settings is null ? null : Guid.NewGuid(), block.Settings)).ToList());

    private static T Element<T>(Action<Mock<T>> setup)
        where T : PublishedElementModel
    {
        var element = new Mock<T>(Mock.Of<IPublishedElement>(), Mock.Of<IPublishedValueFallback>());
        setup(element);
        return element.Object;
    }

    private static RichTextBlock RichText(string html) =>
        Element<RichTextBlock>(block => block.SetupGet(b => b.Text).Returns(new HtmlEncodedString(html)));

    private static CardGridBlock Grid(string heading, string columns, params Card[] cards) => Element<CardGridBlock>(block =>
    {
        block.SetupGet(b => b.Heading).Returns(heading);
        block.SetupGet(b => b.Columns).Returns(columns);
        block.SetupGet(b => b.Cards).Returns(Blocks(cards));
    });

    private static StackerBlock Stacker(string heading, params Card[] cards) => Stacker(heading, null, cards);

    private static StackerBlock Stacker(string heading, string body, params Card[] cards) => Element<StackerBlock>(block =>
    {
        block.SetupGet(b => b.Heading).Returns(heading);
        block.SetupGet(b => b.Body).Returns(body is null ? null : new HtmlEncodedString(body));
        block.SetupGet(b => b.Cards).Returns(Blocks(cards));
    });

    private static Card Card(string heading, string subHeading = null, string body = null, string wash = null, Link link = null) => Element<Card>(card =>
    {
        card.SetupGet(c => c.Heading).Returns(heading);
        card.SetupGet(c => c.SubHeading).Returns(subHeading);
        card.SetupGet(c => c.Body).Returns(body);
        card.SetupGet(c => c.Wash).Returns(wash);
        card.SetupGet(c => c.Link).Returns(link);
    });

    private static SectionSettings Settings(string background, string width) => Element<SectionSettings>(settings =>
    {
        settings.SetupGet(s => s.Background).Returns(background);
        settings.SetupGet(s => s.Width).Returns(width);
    });
}
