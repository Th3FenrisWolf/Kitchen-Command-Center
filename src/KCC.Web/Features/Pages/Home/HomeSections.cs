using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace KCC.Web.Features.Pages.Home;

public static class HomeSections
{
    public static IReadOnlyList<HomeSection> From(BlockListModel sections)
    {
        var tears = new TearCycle();
        var result = new List<HomeSection>();
        foreach (var block in sections ?? Enumerable.Empty<BlockListItem>())
        {
            var section = Section(block.Content, SectionStyle.Classes(block.Settings as SectionSettings), tears);
            if (section is not null)
            {
                result.Add(section);
            }
        }

        return result;
    }

    private static HomeSection Section(IPublishedElement content, string classes, TearCycle tears) => content switch
    {
        RichTextBlock richText => new RichTextSection(classes, tears.Next(), richText.Text?.ToHtmlString() ?? string.Empty),
        CardGridBlock grid => new CardGridSection(
            classes,
            grid.Heading,
            Columns(grid.Columns),
            Elements<Card>(grid.Cards).Select(card => new HomeCard(card.Heading, card.SubHeading, card.Body, SectionStyle.Wash(card.Wash), Link(card.Link), tears.Next())).ToList()),
        StackerBlock stacker => Stacker(stacker, classes, tears),
        _ => null,
    };

    private static StackerSection Stacker(StackerBlock stacker, string classes, TearCycle tears)
    {
        var body = stacker.Body?.ToHtmlString();
        var image = Image(stacker.Image);
        var cards = Elements<Card>(stacker.Cards).ToList();
        var sheetTear = 0;
        if (!string.IsNullOrWhiteSpace(body) || image is not null)
        {
            sheetTear = tears.Next();

            // The sheet is sticky on wide screens, so it passes every card in the stack and comes to rest above the
            // next section. Grids stop at four columns, so its half of the width covers two of that section's sheets
            // or cards at most. So the stack's cards and the next two draws skip its tear, unless another stacker's
            // sheet opens a window of its own first.
            tears.Avoid(sheetTear, cards.Count + 2);
        }

        return new StackerSection(
            classes,
            sheetTear,
            stacker.Heading,
            body,
            image,
            cards.Select(card => new StackerCard(card.Heading, card.SubHeading ?? string.Empty, SectionStyle.Wash(card.Wash), tears.Next())).ToList());
    }

    private static int Columns(string columns) => int.TryParse(columns, out var count) && count is >= 2 and <= 4 ? count : 2;

    private static HomeLink Link(Link link) =>
        link?.Url is { Length: > 0 } url ? new HomeLink(link.Name ?? string.Empty, url, link.Target) : null;

    private static HomeImage Image(MediaWithCrops image) =>
        image is null ? null : new HomeImage(HomeImages.StackerUrl(image), image.Name);

    private static IEnumerable<T> Elements<T>(BlockListModel blocks) =>
        (blocks ?? Enumerable.Empty<BlockListItem>()).Select(block => block.Content).OfType<T>();

    // One tear cycle runs down the whole page, through sheets and cards alike, so a sheet never shares its tear with
    // the one beside it, even across two sections.
    private sealed class TearCycle
    {
        private int taken;
        private int avoided;
        private int drawsLeft;

        public int Next()
        {
            var tear = Take();
            if (drawsLeft > 0)
            {
                drawsLeft--;
                if (tear == avoided)
                {
                    tear = Take();
                }
            }

            return tear;
        }

        public void Avoid(int tear, int draws)
        {
            avoided = tear;
            drawsLeft = draws;
        }

        private int Take() => (taken++ % 6) + 1;
    }
}
