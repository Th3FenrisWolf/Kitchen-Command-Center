using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class GrainTests : BasePageTests
{
    private const string Login = "/account/login";

    private const string Home = "/";

    private const string Library = "/recipes/";

    private const string Recipe = "/recipes/spicy-ramen-flight/";

    private const int PatchWidth = 40;

    private const int PatchHeight = 16;

    private const double Level = 0.5;

    // The sign-in sheet's padding is narrower than a patch plus its torn edge, so its content is hidden and the strip
    // is taken from the middle of the sheet, where nothing but the paper and its rules remain.
    private const string FindPaperStrip = """
        ({ width, height }) => {
          const hide = document.createElement('style')
          hide.textContent = 'main .kcc-sheet > :not(.kcc-washes) { visibility: hidden !important; }'
          document.head.append(hide)
          const sheet = [...document.querySelectorAll('main .kcc-sheet')]
            .find((sheet) => sheet.checkVisibility() && getComputedStyle(sheet).backgroundImage.includes('gradient'))
          if (!sheet) return null
          const box = sheet.getBoundingClientRect()
          return { x: Math.round(box.left + (box.width - width) / 2), y: Math.round(box.top + (box.height - height) / 2) }
        }
        """;

    private const string FindDesk = """
        ({ width, height }) => {
          const desk = (element) =>
            element.matches('#app') ||
            (!element.closest('.kcc-slip, .pad, footer, a, button, input, label') &&
              getComputedStyle(element).backgroundImage === 'none' &&
              ![...element.childNodes].some((node) => node.nodeType === Node.TEXT_NODE && node.data.trim()))
          const lift = document.createElement('style')
          lift.textContent = '* { pointer-events: auto !important; }'
          document.head.append(lift)
          try {
            for (let y = 160; y + height <= innerHeight - 8; y += 8) {
              for (let x = 8; x + width <= innerWidth - 8; x += 8) {
                const points = [[x, y], [x + width, y], [x, y + height], [x + width, y + height], [x + width / 2, y + height / 2]]
                if (points.every(([px, py]) => document.elementsFromPoint(px, py).every(desk))) return { x, y }
              }
            }
            return null
          } finally {
            lift.remove()
          }
        }
        """;

    // Rows and columns crossed by a rule are left out by their mean, then the patch is the middle of the clear run.
    private const string PatchDeviation = """
        async ({ png, width, height, onPaper }) => {
          const image = new Image()
          image.src = `data:image/png;base64,${png}`
          await image.decode()
          const canvas = document.createElement('canvas')
          canvas.width = image.width
          canvas.height = image.height
          const context = canvas.getContext('2d')
          context.drawImage(image, 0, 0)
          const { data } = context.getImageData(0, 0, image.width, image.height)
          const luma = (x, y) => {
            const i = (y * image.width + x) * 4
            return 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2]
          }
          const median = (values) => [...values].sort((a, b) => a - b)[Math.floor(values.length / 2)]
          const longestRun = (clear) => {
            let best = [0, 0]
            for (let start = 0; start < clear.length; ) {
              if (!clear[start]) { start++; continue }
              let end = start
              while (end < clear.length && clear[end]) end++
              if (end - start > best[1] - best[0]) best = [start, end]
              start = end
            }
            return best
          }
          let top = 0
          let left = 0
          if (onPaper) {
            const rows = Array.from({ length: image.height }, (_, y) => Array.from({ length: image.width }, (_, x) => luma(x, y)).reduce((a, b) => a + b) / image.width)
            const rowMedian = median(rows)
            const [rowStart, rowEnd] = longestRun(rows.map((mean) => Math.abs(mean - rowMedian) < 1))
            if (rowEnd - rowStart < height) return { error: `no ${height} rows clear of a rule: rows ${rows.map((r) => r.toFixed(1)).join(' ')}` }
            top = rowStart + Math.floor((rowEnd - rowStart - height) / 2)
            const columns = Array.from({ length: image.width }, (_, x) => Array.from({ length: height }, (_, y) => luma(x, top + y)).reduce((a, b) => a + b) / height)
            const columnMedian = median(columns)
            const [columnStart, columnEnd] = longestRun(columns.map((mean) => Math.abs(mean - columnMedian) < 1))
            if (columnEnd - columnStart < width) return { error: `no ${width} columns clear of a rule` }
            left = columnStart + Math.floor((columnEnd - columnStart - width) / 2)
          }
          const values = []
          for (let y = top; y < top + height; y++) for (let x = left; x < left + width; x++) values.push(luma(x, y))
          const mean = values.reduce((a, b) => a + b) / values.length
          return { deviation: Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length) }
        }
        """;

    private const string GrainAboveASheet = """
        () => {
          const grained = (style) => /url\("?data:image\/svg|repeating-(linear|radial)-gradient/.test(style.backgroundImage)
          const textured = (element) => {
            const style = getComputedStyle(element)
            return grained(style) || /url\(/.test(style.filter)
          }
          const pseudoGrain = (sheet) =>
            [sheet, ...sheet.querySelectorAll('*')].flatMap((element) =>
              ['::before', '::after']
                .filter((pseudo) => grained(getComputedStyle(element, pseudo)))
                .map((pseudo) => `${element.tagName.toLowerCase()}.${[...element.classList].join('.')}${pseudo} in ${sheet.className}`))
          const exempt = (element) => element.matches('.kcc-wash, .kcc-tile, .kcc-tilewrap > .kcc-tape')
          const lift = document.createElement('style')
          lift.textContent = '* { pointer-events: auto !important; }'
          document.head.append(lift)
          try {
            const faults = []
            let sampled = 0
            for (const sheet of document.querySelectorAll('.kcc-sheet')) {
              if (!sheet.checkVisibility()) continue
              const box = sheet.getBoundingClientRect()
              const x = box.left + box.width / 2
              const y = box.top + Math.min(box.height / 2, 24)
              if (x < 0 || y < 0 || x >= innerWidth || y >= innerHeight) continue
              const stack = document.elementsFromPoint(x, y)
              const at = stack.indexOf(sheet)
              if (at < 0) continue
              sampled++
              faults.push(...pseudoGrain(sheet))
              stack.slice(0, at).filter((element) => textured(element) && !exempt(element) && !element.contains(sheet))
                .forEach((element) => faults.push(`${element.tagName.toLowerCase()}.${[...element.classList].join('.')} over ${sheet.className}`))
            }
            return { sampled, faults }
          } finally {
            lift.remove()
          }
        }
        """;

    [Test]
    [Arguments("light", Login)]
    [Arguments("dark", Login)]
    [Arguments("light", Home)]
    [Arguments("dark", Home)]
    [Arguments("light", Library)]
    [Arguments("dark", Library)]
    [Arguments("light", Recipe)]
    [Arguments("dark", Recipe)]
    public async Task BarePaper_HasNoGrain_WhileTheDeskKeepsIts(string ramp, string page)
    {
        await OpenAsync(ramp, page);

        var paper = await Page.EvaluateAsync<Spot?>(FindPaperStrip, new { width = 72, height = 72 });
        var desk = await Page.EvaluateAsync<Spot?>(FindDesk, new { width = PatchWidth, height = PatchHeight });
        _ = await Assert.That(paper).IsNotNull();
        _ = await Assert.That(desk).IsNotNull();

        var onPaper = await DeviationAsync(paper!, 72, 72, onPaper: true);
        var onDesk = await DeviationAsync(desk!, PatchWidth, PatchHeight, onPaper: false);

        _ = await Assert.That(onPaper.Error).IsNull();
        _ = await Assert.That(onPaper.Deviation).IsLessThan(Level);
        _ = await Assert.That(onDesk.Deviation).IsGreaterThan(Level);
    }

    [Test]
    [Arguments("light", Login)]
    [Arguments("dark", Login)]
    [Arguments("light", Home)]
    [Arguments("dark", Home)]
    [Arguments("light", Library)]
    [Arguments("dark", Library)]
    [Arguments("light", Recipe)]
    [Arguments("dark", Recipe)]
    public async Task NoGrainLayer_PaintsAboveASheet(string ramp, string page)
    {
        await OpenAsync(ramp, page);

        var read = await Page.EvaluateAsync<Stacking>(GrainAboveASheet);

        _ = await Assert.That(read.Sampled).IsGreaterThan(0);
        _ = await Assert.That(read.Faults).IsEmpty();
    }

    private async Task OpenAsync(string ramp, string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Page.EmulateMediaAsync(new() { ColorScheme = ramp == "dark" ? ColorScheme.Dark : ColorScheme.Light });
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 0);
        await Washes.FreezeAsync(Page);
        await Page.GotoAsync(page);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", ramp);
        await Page.AddStyleTagAsync(new() { Content = ".kcc-washes { display: none !important; }" });
        await Page.EvaluateAsync("() => document.fonts.ready");
    }

    private async Task<PatchReading> DeviationAsync(Spot spot, int width, int height, bool onPaper)
    {
        var png = await Page.ScreenshotAsync(new() { Clip = new() { X = spot.X, Y = spot.Y, Width = width, Height = height } });
        return await Page.EvaluateAsync<PatchReading>(
            PatchDeviation,
            new { png = Convert.ToBase64String(png), width = PatchWidth, height = PatchHeight, onPaper });
    }

    private sealed class Spot
    {
        public float X { get; set; }

        public float Y { get; set; }
    }

    private sealed class PatchReading
    {
        public double Deviation { get; set; }

        public string? Error { get; set; }
    }

    private sealed class Stacking
    {
        public int Sampled { get; set; }

        public string[] Faults { get; set; } = [];
    }
}
