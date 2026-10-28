using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static partial class Washes
{
    public const int WindowSeconds = 210;

    public const int BloomSeconds = 90;

    // A wash under display: none has no animation and nothing for a reader to see, so only rendered washes are read.
    public const string ReadFields = """
        () => {
          const seconds = (value) => {
            const v = value.trim()
            return v.endsWith('ms') ? parseFloat(v) / 1000 : parseFloat(v)
          }
          const strength = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--wash-strength'))
          const now = document.timeline.currentTime / 1000
          const fields = [...document.querySelectorAll('.kcc-washes')].map((field) => ({
            pad: !!field.closest('.pad-header'),
            washes: [...field.querySelectorAll(':scope > .kcc-wash')].filter((wash) => wash.checkVisibility()).map((wash) => {
              const style = getComputedStyle(wash)
              const animation = wash.getAnimations()[0]
              const delay = seconds(style.animationDelay)
              const start = (animation && animation.startTime !== null ? animation.startTime / 1000 : now) + delay
              const scheduled = style.getPropertyValue('--wash-at').trim()
              // A wash fills backwards only, so once its bloom has run it holds no animation at all.
              const ended = now - start >= seconds(style.animationDuration)
              return {
                id: wash.dataset.wash,
                style: wash.getAttribute('style'),
                at: seconds(scheduled || style.getPropertyValue('--wash-fallback-at')),
                scheduled: scheduled !== '',
                start,
                progress: now - start,
                name: style.animationName,
                opacity: parseFloat(style.opacity),
                peak: strength * parseFloat(style.getPropertyValue('--wash-peak')),
                playState: animation ? animation.playState : ended ? 'finished' : 'none',
                scale: style.scale,
                ink: style.getPropertyValue('--wash-ink').trim(),
                ink2: style.getPropertyValue('--wash-ink-2').trim(),
              }
            }),
          }))
          return fields.filter((field) => field.washes.length > 0)
        }
        """;

    // The library's infinite scroll can fetch another page as soon as the app mounts, adding sheets and their washes.
    public const string SettleResults = """
        async () => {
          const frames = () => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))
          const loading = () => !!document.querySelector('main .fa-circle-notch')
          let count = -1
          for (let look = 0; look < 600; look++) {
            await frames()
            const now = document.querySelectorAll('[data-recipe-name]').length
            if (!loading() && now === count) return
            count = now
          }
          throw new Error('The results never settled')
        }
        """;

    public static async Task<Field[]> ReadAsync(IPage page)
    {
        await page.EvaluateAsync("() => document.fonts.ready");
        return await page.EvaluateAsync<Field[]>(ReadFields);
    }

    public static async Task SettleAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => !!document.querySelector('#app')?.__vue_app__");
        await page.EvaluateAsync(SettleResults);
    }

    // DevTools' animation playback rate holds the document timeline at zero, so every wash stays as the first
    // frame painted it while scripts still run.
    public static async Task FreezeAsync(IPage page)
    {
        var devTools = await page.Context.NewCDPSessionAsync(page);
        await devTools.SendAsync("Animation.enable");
        await devTools.SendAsync("Animation.setPlaybackRate", new Dictionary<string, object> { ["playbackRate"] = 0 });
    }

    public static async Task<double> ElapsedAsync(IPage page)
    {
        var style = await page.Locator("html").GetAttributeAsync("style") ?? string.Empty;
        var match = ElapsedPattern().Match(style);
        return match.Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : double.NaN;
    }

    public static Task<long> SeedAsync(IPage page) =>
        page.EvaluateAsync<long>("() => JSON.parse(document.getElementById('server-content').textContent).bloom.seed");

    [GeneratedRegex(@"--wash-elapsed:\s*([0-9.]+)s")]
    private static partial Regex ElapsedPattern();

    public sealed class Field
    {
        public bool Pad { get; set; }

        public Wash[] Washes { get; set; } = [];
    }

    public sealed class Wash
    {
        public string Id { get; set; } = string.Empty;

        public string Style { get; set; } = string.Empty;

        public double At { get; set; }

        public bool Scheduled { get; set; }

        public double Start { get; set; }

        public double Progress { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Opacity { get; set; }

        public double Peak { get; set; }

        public string PlayState { get; set; } = string.Empty;

        public string Scale { get; set; } = string.Empty;

        public string Ink { get; set; } = string.Empty;

        public string Ink2 { get; set; } = string.Empty;

        public override string ToString() =>
            string.Create(CultureInfo.InvariantCulture, $"{Id} at {At:0.###}s {Name} {PlayState} opacity {Opacity:0.####} peak {Peak:0.####} scale {Scale}");
    }
}
