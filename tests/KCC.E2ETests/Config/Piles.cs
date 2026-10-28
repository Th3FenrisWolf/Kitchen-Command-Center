using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class Piles
{
    // A blank's own computed transform is rotate(t) scale(s), so its matrix's b term, s·sin(t), carries the turn's
    // sign. Offsets ignore transforms, so the blank's box and its sheet's compare untilted.
    private const string ReadPiles = """
        () => {
          const main = document.querySelector('main[role="main"]')
          const blanks = [...main.querySelectorAll('.kcc-blank')].filter((blank) => blank.getClientRects().length > 0)
          const describe = (slip) =>
            `${slip.className} "${slip.textContent.replace(/\s+/g, ' ').trim().slice(0, 40)}"`
          return {
            blanks: blanks.map((blank) => {
              const slip = blank.parentElement
              const torn = slip.querySelector(':scope > .kcc-torn')
              const matrix = getComputedStyle(blank).transform.match(/^matrix\(([^)]+)\)$/)
              const b = matrix ? Number(matrix[1].split(',')[1]) : 0
              const box = (el) => el && [el.offsetLeft, el.offsetTop, el.offsetWidth, el.offsetHeight]
              return {
                sheet: describe(slip),
                sign: Math.sign(b),
                blankBox: box(blank),
                sheetBox: box(torn) ?? [],
              }
            }),
            slips: [...main.querySelectorAll('.kcc-slip')]
              .filter((slip) => slip.getClientRects().length > 0)
              .filter((slip) => ![...slip.classList].some((name) => name.startsWith('pad-')))
              .map((slip) => ({
                sheet: describe(slip),
                blanks: slip.querySelectorAll(':scope > .kcc-blank').length,
                blankLast: !!slip.lastElementChild?.matches('.kcc-blank'),
                width: slip.querySelector(':scope > .kcc-torn')?.offsetWidth ?? 0,
                wide: slip.classList.contains('kcc-slip--wide'),
                hero: [...slip.classList].some((name) => name.startsWith('kcc-tear-hero')),
              })),
            scrollWidth: document.documentElement.scrollWidth,
            clientWidth: document.documentElement.clientWidth,
          }
        }
        """;

    private const string ReadTurn = """
        (slip) => {
          const matrix = getComputedStyle(slip.querySelector(':scope > .kcc-blank')).transform.match(/^matrix\(([^)]+)\)$/)
          return Math.sign(matrix ? Number(matrix[1].split(',')[1]) : 0)
        }
        """;

    private const string ReadOverlay = """
        (overlay) => {
          const slip = overlay.querySelector('.kcc-slip')
          const blank = slip?.querySelector(':scope > .kcc-blank')
          const matrix = blank && getComputedStyle(blank).transform.match(/^matrix\(([^)]+)\)$/)
          return {
            blanks: overlay.querySelectorAll('.kcc-blank').length,
            turn: matrix ? Number(matrix[1].split(',')[1]) : 0,
            wide: !!slip?.classList.contains('kcc-slip--wide'),
            tilt: slip ? getComputedStyle(slip).getPropertyValue('--r').trim() : '',
          }
        }
        """;

    private static readonly double WideTurn = 1.005 * Math.Sin(0.5 * Math.PI / 180);

    private const double TurnTolerance = 0.0005;

    private const int MeasuringWidth = 1280;

    private const double WideFrom = 700;

    private const double WidthTolerance = 4;

    public static Task<int> TurnAsync(ILocator slip) => slip.EvaluateAsync<int>(ReadTurn);

    public static async Task<IReadOnlyList<string>> FaultsAsync(IPage page)
    {
        var walk = await page.EvaluateAsync<Walk>(ReadPiles);
        var faults = new List<string>();

        if (walk.Blanks.Length == 0)
        {
            faults.Add("no visible blank in main");
        }

        for (var i = 0; i < walk.Blanks.Length; i++)
        {
            var blank = walk.Blanks[i];
            if (blank.Sign == 0)
            {
                faults.Add($"#{i} {blank.Sheet}: the blank does not turn");
            }
            else if (i > 0 && blank.Sign == walk.Blanks[i - 1].Sign)
            {
                faults.Add($"#{i} {blank.Sheet}: turns {Sign(blank.Sign)}, the same way as #{i - 1} {walk.Blanks[i - 1].Sheet}");
            }

            if (!blank.BlankBox.SequenceEqual(blank.SheetBox))
            {
                faults.Add($"#{i} {blank.Sheet}: blank box [{string.Join(", ", blank.BlankBox)}] is not its sheet's [{string.Join(", ", blank.SheetBox)}]");
            }
        }

        foreach (var slip in walk.Slips)
        {
            if (slip.Blanks != 1 || !slip.BlankLast)
            {
                faults.Add($"{slip.Sheet}: holds {slip.Blanks} blanks{(slip.BlankLast ? string.Empty : ", and its last child is not a blank")}");
            }
        }

        if (page.ViewportSize?.Width == MeasuringWidth)
        {
            faults.AddRange(WideFaults(walk.Slips));
        }

        if (walk.ScrollWidth > walk.ClientWidth)
        {
            faults.Add($"the page scrolls sideways: scrollWidth {walk.ScrollWidth} > clientWidth {walk.ClientWidth}");
        }

        if (faults.Count > 0)
        {
            faults.Insert(0, $"signs in reading order: {string.Concat(walk.Blanks.Select(blank => Sign(blank.Sign)))}");
        }

        return faults;
    }

    public static async Task<IReadOnlyList<string>> OverlayFaultsAsync(ILocator overlay)
    {
        var sheet = await overlay.EvaluateAsync<OverlaySheet>(ReadOverlay);
        var faults = new List<string>();

        if (sheet.Blanks != 1)
        {
            faults.Add($"the overlay holds {sheet.Blanks} blanks, not one");
        }

        if (Math.Abs(Math.Abs(sheet.Turn) - WideTurn) > TurnTolerance)
        {
            faults.Add($"the overlay's blank turns by b = {sheet.Turn}, not about ±{WideTurn:F4}");
        }

        if (!sheet.Wide)
        {
            faults.Add("the overlay's sheet is not kcc-slip--wide");
        }

        if (sheet.Tilt != "0")
        {
            faults.Add($"the overlay's sheet tilts: --r is '{sheet.Tilt}'");
        }

        return faults;
    }

    private static IEnumerable<string> WideFaults(IEnumerable<Slip> slips)
    {
        foreach (var slip in slips.Where(slip => slip.Blanks > 0))
        {
            var mustBeWide = slip.Hero || slip.Width > WideFrom + WidthTolerance;
            var mustNotBeWide = !slip.Hero && slip.Width <= WideFrom - WidthTolerance;

            if (mustBeWide && !slip.Wide)
            {
                yield return $"{slip.Sheet}: {slip.Width}px wide at {MeasuringWidth}px, not kcc-slip--wide";
            }
            else if (mustNotBeWide && slip.Wide)
            {
                yield return $"{slip.Sheet}: {slip.Width}px wide at {MeasuringWidth}px, but kcc-slip--wide";
            }
        }
    }

    private static string Sign(int sign) => sign switch
    {
        > 0 => "+",
        < 0 => "-",
        _ => "0",
    };

    private sealed class Walk
    {
        public Blank[] Blanks { get; init; } = [];

        public Slip[] Slips { get; init; } = [];

        public double ScrollWidth { get; init; }

        public double ClientWidth { get; init; }
    }

    private sealed class OverlaySheet
    {
        public int Blanks { get; init; }

        public double Turn { get; init; }

        public bool Wide { get; init; }

        public string Tilt { get; init; } = string.Empty;
    }

    private sealed class Blank
    {
        public string Sheet { get; init; } = string.Empty;

        public int Sign { get; init; }

        public double[] BlankBox { get; init; } = [];

        public double[] SheetBox { get; init; } = [];
    }

    private sealed class Slip
    {
        public string Sheet { get; init; } = string.Empty;

        public int Blanks { get; init; }

        public bool BlankLast { get; init; }

        public double Width { get; init; }

        public bool Wide { get; init; }

        public bool Hero { get; init; }
    }
}
