using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public sealed class Measurement
{
    public Dictionary<string, double> Values { get; } = [];

    public List<string> Gaps { get; } = [];

    public void Gap(string metric, string reason) => Gaps.Add($"{metric}: {reason}");

    public void Merge(Measurement other)
    {
        foreach (var (name, value) in other.Values)
        {
            Values[name] = value;
        }

        Gaps.AddRange(other.Gaps);
    }
}

public sealed record CollectOptions(
    string? StorageState = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    bool Timing = true,
    bool Diagnostics = true,
    bool Freeze = false,
    SitePaths? Paths = null,
    LiveSite? Live = null);

public readonly record struct Box(double X, double Y, double Width, double Height);

public sealed class FrameStats
{
    public const double WithinBudgetMs = 16.7;

    public const double LongFrameMs = 50;

    public static Dictionary<string, double> Summarize(string prefix, IReadOnlyList<double> deltas)
    {
        var frames = deltas.Skip(1).ToList();
        if (frames.Count == 0)
        {
            return [];
        }

        var sorted = frames.Order().ToList();
        return new()
        {
            [$"{prefix}frame_p95_ms"] = Math.Round(sorted[Math.Min(sorted.Count - 1, (int)(0.95 * sorted.Count))], 1),
            [$"{prefix}frame_max_ms"] = Math.Round(sorted[^1], 1),
            [$"{prefix}frames_within_16_7_pct"] = Math.Round(100.0 * frames.Count(delta => delta <= WithinBudgetMs) / frames.Count, 1),
            [$"{prefix}long_frames"] = frames.Count(delta => delta > LongFrameMs),
        };
    }
}

public sealed record LayoutShiftEntry(double Value, double Start);

public static class LayoutShift
{
    public static (double Load, double Scroll) Split(IEnumerable<LayoutShiftEntry> shifts, double? scrollFrom)
    {
        var load = 0.0;
        var scroll = 0.0;
        foreach (var shift in shifts)
        {
            if (scrollFrom is { } from && shift.Start >= from)
            {
                scroll += shift.Value;
            }
            else
            {
                load += shift.Value;
            }
        }

        return (Math.Round(load, 4), Math.Round(scroll, 4));
    }
}

public static class ResourceWeights
{
    public static int GzipBytes(byte[] body)
    {
        using var sink = new CountingStream();
        using (var gzip = new GZipStream(sink, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(body);
        }

        return (int)sink.Length;
    }

    public static Dictionary<string, double> Totals(IEnumerable<(string Type, byte[] Body)> resources)
    {
        double css = 0, js = 0, fonts = 0, images = 0;
        foreach (var (type, body) in resources)
        {
            switch (type)
            {
                case "stylesheet":
                    css += GzipBytes(body);
                    break;
                case "script":
                    js += GzipBytes(body);
                    break;
                case "font":
                    fonts += body.Length;
                    break;
                case "image":
                    images += body.Length;
                    break;
            }
        }

        return new() { ["css_gz_bytes"] = css, ["js_gz_bytes"] = js, ["font_bytes"] = fonts, ["image_bytes"] = images };
    }

    private sealed class CountingStream : Stream
    {
        private long length;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position { get => length; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => length += count;
    }
}

public static class TraceTotals
{
    private static readonly Dictionary<string, string> Metrics = new()
    {
        ["Paint"] = "trace_paint_ms",
        ["RasterTask"] = "trace_raster_ms",
        ["CompositeLayers"] = "trace_composite_ms",
        ["Commit"] = "trace_composite_ms",
        ["UpdateLayerTree"] = "trace_update_layer_tree_ms",
        ["UpdateLayoutTree"] = "trace_update_layer_tree_ms",
        ["Layout"] = "trace_layout_ms",
    };

    public static Dictionary<string, double> Summarize(IEnumerable<JsonElement> events)
    {
        var totals = Metrics.Values.Distinct().ToDictionary(name => name, _ => 0.0);
        var open = new Dictionary<string, Stack<double>>();
        foreach (var traceEvent in events)
        {
            var name = traceEvent.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            if (!Metrics.TryGetValue(name, out var metric))
            {
                continue;
            }

            var phase = traceEvent.TryGetProperty("ph", out var ph) ? ph.GetString() : null;
            var thread = $"{Read(traceEvent, "pid")}:{Read(traceEvent, "tid")}:{name}";
            switch (phase)
            {
                case "X" when traceEvent.TryGetProperty("dur", out var dur):
                    totals[metric] += dur.GetDouble() / 1000;
                    break;
                case "B":
                    if (!open.TryGetValue(thread, out var stack))
                    {
                        open[thread] = stack = new();
                    }

                    stack.Push(Read(traceEvent, "ts"));
                    break;
                case "E" when open.TryGetValue(thread, out var started) && started.Count > 0:
                    totals[metric] += (Read(traceEvent, "ts") - started.Pop()) / 1000;
                    break;
            }
        }

        return totals.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 1));
    }

    private static double Read(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;
}

public static class GeometryShift
{
    public const string ReadBoxes = """
        () => {
          const seen = new Map()
          const boxes = {}
          for (const element of document.querySelectorAll('header, main, footer, nav, .kcc-sheet, .kcc-slip')) {
            const base = element.tagName + '.' + element.className
            const ordinal = seen.get(base) ?? 0
            seen.set(base, ordinal + 1)
            const rect = element.getBoundingClientRect()
            boxes[base + '#' + ordinal] = [rect.left + scrollX, rect.top + scrollY, rect.width, rect.height]
          }
          return boxes
        }
        """;

    public static double Score(IReadOnlyDictionary<string, double[]> before, IReadOnlyDictionary<string, double[]> after, double viewportWidth, double viewportHeight)
    {
        var viewport = new Box(0, 0, viewportWidth, viewportHeight);
        var score = 0.0;
        foreach (var (key, was) in before)
        {
            if (!after.TryGetValue(key, out var now))
            {
                continue;
            }

            var distance = Math.Max(Math.Abs(now[0] - was[0]), Math.Abs(now[1] - was[1])) / Math.Max(viewportWidth, viewportHeight);
            if (distance == 0)
            {
                continue;
            }

            var impact = (VisibleArea(was, viewport) + VisibleArea(now, viewport) - OverlapArea(was, now, viewport)) / (viewportWidth * viewportHeight);
            score += impact * distance;
        }

        return Math.Round(score, 4);
    }

    private static double VisibleArea(double[] box, Box viewport) => Intersect(box, [viewport.X, viewport.Y, viewport.Width, viewport.Height]);

    private static double OverlapArea(double[] a, double[] b, Box viewport)
    {
        var overlap = Overlap(a, b);
        return overlap is null ? 0 : VisibleArea(overlap, viewport);
    }

    private static double[]? Overlap(double[] a, double[] b)
    {
        var left = Math.Max(a[0], b[0]);
        var top = Math.Max(a[1], b[1]);
        var right = Math.Min(a[0] + a[2], b[0] + b[2]);
        var bottom = Math.Min(a[1] + a[3], b[1] + b[3]);
        return right > left && bottom > top ? [left, top, right - left, bottom - top] : null;
    }

    private static double Intersect(double[] a, double[] b) => Overlap(a, b) is { } box ? box[2] * box[3] : 0;
}

public sealed class CaseSession : IAsyncDisposable
{
    private const string Observers = """
        (() => {
          const state = { lcp: 0, shifts: [], events: [], loaf: [], supported: PerformanceObserver.supportedEntryTypes ?? [] }
          const watch = (type, take, extra) => {
            try {
              new PerformanceObserver((list) => list.getEntries().forEach(take)).observe({ type, buffered: true, ...extra })
            } catch {}
          }
          watch('largest-contentful-paint', (entry) => { state.lcp = entry.startTime })
          watch('layout-shift', (entry) => { if (!entry.hadRecentInput) state.shifts.push({ value: entry.value, start: entry.startTime }) })
          watch('event', (entry) => state.events.push(entry.duration), { durationThreshold: 16 })
          watch('long-animation-frame', (entry) => state.loaf.push(entry.startTime))
          let frames = []
          let running = false
          let begun = 0
          state.record = () => {
            frames = []
            running = true
            begun = performance.now()
            let last = begun
            const tick = (now) => {
              frames.push(now - last)
              last = now
              if (running) requestAnimationFrame(tick)
            }
            requestAnimationFrame((now) => { last = now; requestAnimationFrame(tick) })
          }
          state.stop = () => {
            running = false
            return { frames, loaf: state.loaf.filter((start) => start >= begun).length }
          }
          window.__kcc = state
        })()
        """;

    private readonly List<Task<(string Type, string Url, byte[]? Body)>> bodies = [];

    private CaseSession(Case subject, IBrowserContext context, IPage page, ICDPSession? devTools)
    {
        Case = subject;
        Context = context;
        Page = page;
        DevTools = devTools;
    }

    public Case Case { get; }

    public IBrowserContext Context { get; }

    public IPage Page { get; }

    public ICDPSession? DevTools { get; }

    public Profile Profile => Profiles.ByName(Case.Profile);

    public Measurement Measurement { get; } = new();

    public Dictionary<string, double[]> InitialBoxes { get; private set; } = [];

    public static async Task<CaseSession> OpenAsync(IBrowser browser, Uri baseUrl, Case subject, CollectOptions options)
    {
        var profile = Profiles.ByName(subject.Profile);
        var contextOptions = Profiles.ContextOptions(profile, subject.Engine, subject.Ramp, baseUrl);
        contextOptions.StorageState = options.StorageState;
        contextOptions.ExtraHTTPHeaders = options.Headers;
        contextOptions.ReducedMotion = subject.Motion == Motion.Reduced ? ReducedMotion.Reduce : ReducedMotion.NoPreference;
        var context = await browser.NewContextAsync(contextOptions);
        try
        {
            var blocked = new List<string>();
            if (options.Live is { } live)
            {
                await live.GuardAsync(context, signingIn: false, reason => { lock (blocked) { blocked.Add(reason); } });
            }

            _ = await subject.BeginVisitAsync(context, baseUrl);
            await context.AddInitScriptAsync(Observers);

            var page = await context.NewPageAsync();
            var devTools = subject.Engine == Engine.Chromium ? await context.NewCDPSessionAsync(page) : null;
            var session = new CaseSession(subject, context, page, devTools);
            page.Response += (_, response) => session.Track(response);
            if (!subject.ImagesOn)
            {
                await page.RouteAsync("**/*", route => route.Request.ResourceType == "image" ? route.AbortAsync() : route.FallbackAsync());
            }

            if (devTools is not null && options.Timing)
            {
                await Profiles.ThrottleAsync(devTools, profile);
            }

            if (options.Freeze)
            {
                await Washes.FreezeAsync(page);
            }

            await CasePages.OpenAsync(page, subject.Page, options.Paths);
            if (CasePages.SessionRefused(subject, page.Url))
            {
                throw new InvalidOperationException("the site sent the member to the sign-in page; it refused the member's session");
            }

            await session.SettleAsync();
            if (CasePages.Navigates(subject.Page))
            {
                session.ForgetBodies();
                _ = await CasePages.ReachStateAsync(page, subject.Page, options.Paths);
                session.Measurement.Values.Clear();
                session.Measurement.Gaps.Clear();
                await session.SettleAsync();
            }
            else if (await CasePages.ReachStateAsync(page, subject.Page, options.Paths) is { } unreached)
            {
                session.Measurement.Gap("state", unreached);
            }

            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(1500);
            await session.RequireRampAsync();
            lock (blocked)
            {
                session.Measurement.Gaps.AddRange(blocked);
            }

            return session;
        }
        catch
        {
            await context.CloseAsync();
            throw;
        }
    }

    public async Task<Dictionary<string, double>> ResourceTotalsAsync()
    {
        Task<(string Type, string Url, byte[]? Body)>[] pending;
        lock (bodies)
        {
            pending = [.. bodies];
        }

        var finished = await Task.WhenAll(pending);
        return ResourceWeights.Totals(finished
            .Where(item => item.Body is not null)
            .DistinctBy(item => item.Url)
            .Select(item => (item.Type, item.Body!)));
    }

    public async ValueTask DisposeAsync() => await Context.CloseAsync();

    private void Track(IResponse response)
    {
        lock (bodies)
        {
            bodies.Add(ReadBodyAsync(response));
        }
    }

    private void ForgetBodies()
    {
        lock (bodies)
        {
            bodies.Clear();
        }
    }

    private async Task RequireRampAsync()
    {
        var theme = await Page.EvaluateAsync<string?>("() => document.documentElement.dataset.theme ?? null");
        if (theme != Case.Wire(Case.Ramp))
        {
            throw new InvalidOperationException($"the page rendered the {theme ?? "unset"} ramp, not {Case.Wire(Case.Ramp)}");
        }
    }

    private static async Task<(string Type, string Url, byte[]? Body)> ReadBodyAsync(IResponse response)
    {
        try
        {
            return (response.Request.ResourceType, response.Url, await response.BodyAsync());
        }
        catch (PlaywrightException)
        {
            return (response.Request.ResourceType, response.Url, null);
        }
    }

    private async Task SettleAsync()
    {
        try
        {
            await Page.WaitForFunctionAsync("() => performance.getEntriesByName('kcc:hydrated').length > 0", null, new() { Timeout = 90_000 });
            Measurement.Values["hydrated_ms"] = Math.Round(await Page.EvaluateAsync<double>("() => performance.getEntriesByName('kcc:hydrated')[0].startTime"), 1);
        }
        catch (TimeoutException)
        {
            Measurement.Gap("hydrated_ms", "the kcc:hydrated mark never appeared");
        }

        Measurement.Values["server_response_ms"] = Math.Round(
            await Page.EvaluateAsync<double>("() => { const n = performance.getEntriesByType('navigation')[0]; return n ? n.responseStart - n.requestStart : 0 }"), 1);
        InitialBoxes = await Collectors.ReadAsync<Dictionary<string, double[]>>(Page, GeometryShift.ReadBoxes);
        await Washes.SettleAsync(Page);
        await Page.EvaluateAsync("() => { window.__kcc.loadEnd = performance.now() }");
    }
}

public static class Collectors
{
    private const string TraceCategories =
        "toplevel,devtools.timeline,disabled-by-default-devtools.timeline,disabled-by-default-devtools.timeline.frame,benchmark,cc,viz";

    private const int IdleSeconds = 10;

    private const int BloomWindowSeconds = 5;

    public static async Task<Measurement> MeasureAsync(IBrowser browser, Uri baseUrl, Case subject, CollectOptions options)
    {
        await using var session = await CaseSession.OpenAsync(browser, baseUrl, subject, options);
        var result = session.Measurement;
        result.Merge(await DeterministicAsync(session));
        if (options.Timing)
        {
            result.Merge(await TimingAsync(browser, session, options.Diagnostics));
            if (!options.Diagnostics)
            {
                return result;
            }

            result.Merge(await GeometryAsync(browser, baseUrl, session, options));
            if (subject.Engine == Engine.Chromium)
            {
                result.Merge(await CssUsageAsync(browser, baseUrl, subject, options));
            }
        }

        return result;
    }

    public static async Task<Measurement> DeterministicAsync(CaseSession session)
    {
        var result = new Measurement();
        foreach (var (name, value) in await DeterministicProbes.ReadAsync(session.Page))
        {
            result.Values[name] = value;
        }

        foreach (var (name, value) in await session.ResourceTotalsAsync())
        {
            result.Values[name] = value;
        }

        if (session.DevTools is null)
        {
            result.Gap("layers", "WebKit exposes no layer tree");
            result.Gap("layer_area_mpx", "WebKit exposes no layer tree");
        }
        else if (await ReadLayersAsync(session) is { } layers)
        {
            result.Values["layers"] = layers.Count;
            result.Values["layer_area_mpx"] = layers.AreaMpx;
        }
        else
        {
            result.Gap("layers", "the layer tree never reported");
        }

        return result;
    }

    public static async Task<Measurement> TimingAsync(IBrowser browser, CaseSession session, bool diagnostics = true)
    {
        var result = new Measurement();
        var page = session.Page;
        var chromium = session.DevTools is not null;
        if (!chromium)
        {
            foreach (var metric in new[] { "lcp_ms", "idle_gpu_pct", "trace_paint_ms", "trace_raster_ms", "trace_composite_ms", "trace_update_layer_tree_ms", "trace_layout_ms", "css_unused_bytes" })
            {
                result.Gap(metric, "WebKit has no CDP, no LCP and no trace; its numbers are unthrottled");
            }
        }

        if (session.Case.Visit == VisitState.Mid)
        {
            result.Merge(await BloomWindowAsync(page, chromium));
        }

        if (chromium && diagnostics)
        {
            await IdleGpuAsync(browser, session, result);
        }

        var scroll = await ScrollAsync(session);
        result.Merge(scroll);
        await HoverAsync(session, result);
        await ReadObserversAsync(session, result);
        return result;
    }

    private static async Task<Measurement> BloomWindowAsync(IPage page, bool chromium)
    {
        var result = new Measurement();
        await page.Mouse.MoveAsync(1, 1);
        await page.EvaluateAsync("() => window.__kcc.record()");
        await page.WaitForTimeoutAsync(BloomWindowSeconds * 1000);
        var window = await ReadAsync<FrameWindow>(page, "() => window.__kcc.stop()");
        foreach (var (name, value) in FrameStats.Summarize("bloom_", window.Frames))
        {
            result.Values[name] = value;
        }

        if (chromium)
        {
            result.Values["bloom_loaf_count"] = window.Loaf;
        }

        return result;
    }

    private static async Task IdleGpuAsync(IBrowser browser, CaseSession session, Measurement result)
    {
        var devTools = session.DevTools!;
        await Profiles.SetCpuAsync(devTools, 1);
        await session.Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await session.Page.Mouse.MoveAsync(1, 1);
        await session.Page.WaitForTimeoutAsync(500);
        var browserSession = await browser.NewBrowserCDPSessionAsync();
        try
        {
            var before = await GpuSecondsAsync(browserSession);
            var timer = Stopwatch.StartNew();
            await session.Page.WaitForTimeoutAsync(IdleSeconds * 1000);
            var after = await GpuSecondsAsync(browserSession);
            if (before is { } start && after is { } end)
            {
                result.Values["idle_gpu_pct"] = Math.Round(100 * (end - start) / timer.Elapsed.TotalSeconds, 1);
            }
            else
            {
                result.Gap("idle_gpu_pct", "the browser reported no GPU process");
            }
        }
        finally
        {
            await browserSession.DetachAsync();
            await Profiles.SetCpuAsync(devTools, session.Profile.CpuSlowdown);
        }
    }

    private static async Task<double?> GpuSecondsAsync(ICDPSession browserSession)
    {
        var info = await browserSession.SendAsync("SystemInfo.getProcessInfo");
        if (info is not { } element || !element.TryGetProperty("processInfo", out var processes))
        {
            return null;
        }

        var gpu = processes.EnumerateArray().Where(process => process.GetProperty("type").GetString() == "GPU").ToList();
        return gpu.Count == 0 ? null : gpu.Sum(process => process.GetProperty("cpuTime").GetDouble());
    }

    private static async Task<Measurement> ScrollAsync(CaseSession session)
    {
        var result = new Measurement();
        var page = session.Page;
        var events = new List<JsonElement>();
        var devTools = session.DevTools;
        Task? completed = null;
        if (devTools is not null)
        {
            devTools.Event("Tracing.dataCollected").OnEvent += (_, args) =>
            {
                if (args is { } data && data.TryGetProperty("value", out var batch))
                {
                    events.AddRange(batch.EnumerateArray().Select(item => item.Clone()));
                }
            };
            var done = new TaskCompletionSource();
            devTools.Event("Tracing.tracingComplete").OnEvent += (_, _) => done.TrySetResult();
            completed = done.Task;
            _ = await devTools.SendAsync("Tracing.start", new Dictionary<string, object>
            {
                ["categories"] = TraceCategories,
                ["transferMode"] = "ReportEvents",
            });
        }

        var profile = session.Profile;
        await page.Mouse.MoveAsync(profile.Width / 2, profile.Height / 2);
        await page.EvaluateAsync("() => window.__kcc.record()");
        await page.WaitForTimeoutAsync(300);
        var distance = await page.EvaluateAsync<double>("() => document.documentElement.scrollHeight - innerHeight");
        const int Step = 100;
        var steps = (int)Math.Ceiling(distance / Step);
        for (var direction = 1; direction >= -1; direction -= 2)
        {
            for (var step = 0; step < steps; step++)
            {
                await page.Mouse.WheelAsync(0, direction * Step);
                await page.WaitForTimeoutAsync(66);
            }
        }

        await page.WaitForTimeoutAsync(300);
        var window = await ReadAsync<FrameWindow>(page, "() => window.__kcc.stop()");
        foreach (var (name, value) in FrameStats.Summarize(string.Empty, window.Frames))
        {
            result.Values[name] = value;
        }

        if (devTools is not null)
        {
            result.Values["loaf_count"] = window.Loaf;
        }

        if (devTools is not null && completed is not null)
        {
            _ = await devTools.SendAsync("Tracing.end");
            await completed.WaitAsync(TimeSpan.FromSeconds(30));
            foreach (var (name, value) in TraceTotals.Summarize(events))
            {
                result.Values[name] = value;
            }
        }

        return result;
    }

    private static async Task HoverAsync(CaseSession session, Measurement result)
    {
        var page = session.Page;
        var cards = page.Locator(".kcc-slip--lift");
        var count = Math.Min(6, await cards.CountAsync());
        if (count == 0)
        {
            return;
        }

        await page.EvaluateAsync("""
            () => {
              window.__kccHover = []
              document.addEventListener('pointerover', (event) => {
                const begun = event.timeStamp
                requestAnimationFrame(() => setTimeout(() => window.__kccHover.push(performance.now() - begun), 0))
              }, { capture: true })
            }
            """);
        for (var index = 0; index < count; index++)
        {
            var card = cards.Nth(index);
            await card.ScrollIntoViewIfNeededAsync();
            await page.WaitForTimeoutAsync(400);
            if (await card.BoundingBoxAsync() is not { } box)
            {
                continue;
            }

            await page.Mouse.MoveAsync(2, 2);
            await page.WaitForTimeoutAsync(250);
            await page.Mouse.MoveAsync((float)(box.X + (box.Width / 2)), (float)(box.Y + (box.Height / 2)), new() { Steps = 2 });
            await page.WaitForTimeoutAsync(450);
        }

        await page.WaitForTimeoutAsync(300);
    }

    private static async Task ReadObserversAsync(CaseSession session, Measurement result)
    {
        var reading = await ReadAsync<ObserverReading>(session.Page,
            "() => ({ lcp: __kcc.lcp, shifts: __kcc.shifts, loadEnd: __kcc.loadEnd ?? null, events: __kcc.events, supported: __kcc.supported, hover: window.__kccHover ?? [] })");
        var eventTiming = reading.Supported.Contains("event");
        if (session.DevTools is not null)
        {
            result.Values["lcp_ms"] = Math.Round(reading.Lcp, 1);
            var (load, scroll) = LayoutShift.Split(reading.Shifts, reading.LoadEnd);
            result.Values["cls"] = load;
            result.Values["cls_scroll"] = scroll;
        }
        else
        {
            result.Gap("cls", "WebKit has no layout-shift entries; cls_geometry stands in");
        }

        if (eventTiming)
        {
            result.Values["interaction_ms_max"] = reading.Events.Count == 0 ? 0 : Math.Round(reading.Events.Max(), 1);
        }
        else if (reading.Hover.Count > 0)
        {
            result.Gap("interaction_ms_max", "Event Timing is unsupported; the value is the worst pointerover-to-next-frame delay");
            result.Values["interaction_ms_max"] = Math.Round(reading.Hover.Max(), 1);
        }
    }

    private static async Task<Measurement> GeometryAsync(IBrowser browser, Uri baseUrl, CaseSession session, CollectOptions options)
    {
        var result = new Measurement();
        if (session.Case.Page is Pages.VariantCook or Pages.AddVariant)
        {
            result.Gap("cls_geometry", "the page's state cannot be reached with scripts off");
            return result;
        }

        var profile = session.Profile;
        var contextOptions = Profiles.ContextOptions(profile, session.Case.Engine, session.Case.Ramp, baseUrl);
        contextOptions.StorageState = options.StorageState;
        contextOptions.ExtraHTTPHeaders = options.Headers;
        contextOptions.JavaScriptEnabled = false;
        await using var context = await browser.NewContextAsync(contextOptions);
        await GuardAsync(context, options, result);
        _ = await session.Case.BeginVisitAsync(context, baseUrl);
        var page = await context.NewPageAsync();
        _ = await page.GotoAsync(CasePages.Path(session.Case.Page, options.Paths), new() { WaitUntil = WaitUntilState.Load });
        await page.EvaluateAsync("() => document.fonts.ready");
        var withoutScripts = await ReadAsync<Dictionary<string, double[]>>(page, GeometryShift.ReadBoxes);
        result.Values["cls_geometry"] = GeometryShift.Score(withoutScripts, session.InitialBoxes, profile.Width, profile.Height);
        return result;
    }

    private static async Task<Measurement> CssUsageAsync(IBrowser browser, Uri baseUrl, Case subject, CollectOptions options)
    {
        var result = new Measurement();
        var profile = Profiles.ByName(subject.Profile);
        var contextOptions = Profiles.ContextOptions(profile, subject.Engine, subject.Ramp, baseUrl);
        contextOptions.StorageState = options.StorageState;
        contextOptions.ExtraHTTPHeaders = options.Headers;
        await using var context = await browser.NewContextAsync(contextOptions);
        await GuardAsync(context, options, result);
        _ = await subject.BeginVisitAsync(context, baseUrl);
        var page = await context.NewPageAsync();
        var devTools = await context.NewCDPSessionAsync(page);
        var sheetLengths = new Dictionary<string, double>();
        devTools.Event("CSS.styleSheetAdded").OnEvent += (_, args) =>
        {
            if (args is { } added && added.GetProperty("header") is var header && header.GetProperty("origin").GetString() == "regular")
            {
                sheetLengths[header.GetProperty("styleSheetId").GetString()!] = header.GetProperty("length").GetDouble();
            }
        };
        _ = await devTools.SendAsync("DOM.enable");
        _ = await devTools.SendAsync("CSS.enable");
        _ = await devTools.SendAsync("CSS.startRuleUsageTracking");
        await CasePages.OpenAsync(page, subject.Page, options.Paths);
        await Washes.SettleAsync(page);
        _ = await CasePages.ReachStateAsync(page, subject.Page, options.Paths);
        await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");
        await page.WaitForTimeoutAsync(700);
        var usage = await devTools.SendAsync("CSS.stopRuleUsageTracking");
        var used = new Dictionary<string, List<(double Start, double End)>>();
        foreach (var rule in usage!.Value.GetProperty("ruleUsage").EnumerateArray().Where(rule => rule.GetProperty("used").GetBoolean()))
        {
            var sheet = rule.GetProperty("styleSheetId").GetString()!;
            if (!used.TryGetValue(sheet, out var ranges))
            {
                used[sheet] = ranges = [];
            }

            ranges.Add((rule.GetProperty("startOffset").GetDouble(), rule.GetProperty("endOffset").GetDouble()));
        }

        var total = sheetLengths.Values.Sum();
        var unused = total - used.Sum(pair => UnionLength(pair.Value));
        result.Values["css_unused_bytes"] = unused;
        result.Values["css_unused_pct"] = total == 0 ? 0 : Math.Round(100 * unused / total, 1);
        return result;
    }

    private static double UnionLength(List<(double Start, double End)> ranges)
    {
        var length = 0.0;
        var reached = double.NegativeInfinity;
        foreach (var (start, end) in ranges.OrderBy(range => range.Start))
        {
            if (end <= reached)
            {
                continue;
            }

            length += end - Math.Max(start, reached);
            reached = end;
        }

        return length;
    }

    private static async Task<(int Count, double AreaMpx)?> ReadLayersAsync(CaseSession session)
    {
        var devTools = session.DevTools!;
        JsonElement? latest = null;
        devTools.Event("LayerTree.layerTreeDidChange").OnEvent += (_, args) =>
        {
            if (args is { } data && data.TryGetProperty("layers", out var layers))
            {
                latest = layers.Clone();
            }
        };
        _ = await devTools.SendAsync("LayerTree.enable");

        // A page that cannot scroll and has nothing animating never commits a new layer tree, so a promoted probe is
        // added and removed; the tree reported after its removal is the page's own.
        await session.Page.EvaluateAsync("""
            async () => {
              const frame = () => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))
              scrollBy(0, 1)
              const probe = document.createElement('div')
              probe.style.cssText = 'position:fixed;top:0;left:0;width:1px;height:1px;will-change:transform'
              document.body.append(probe)
              await frame()
              probe.remove()
              scrollBy(0, -1)
              await frame()
            }
            """);
        await session.Page.WaitForTimeoutAsync(500);
        _ = await devTools.SendAsync("LayerTree.disable");
        if (latest is not { } reported)
        {
            return null;
        }

        var layers = reported.EnumerateArray().ToList();
        var area = layers
            .Where(layer => layer.TryGetProperty("drawsContent", out var draws) && draws.GetBoolean())
            .Sum(layer => layer.GetProperty("width").GetDouble() * layer.GetProperty("height").GetDouble());
        return (layers.Count, Math.Round(area / 1e6, 1));
    }

    public static async Task<string> SignInStateAsync(IBrowser browser, Uri baseUrl, Profile profile, Engine engine, string usernameVariable, string passwordVariable, IReadOnlyDictionary<string, string>? headers = null, LiveSite? live = null)
    {
        var contextOptions = Profiles.ContextOptions(profile, engine, Ramp.Light, baseUrl);
        contextOptions.ExtraHTTPHeaders = headers;
        await using var context = await browser.NewContextAsync(contextOptions);
        if (live is not null)
        {
            await live.GuardAsync(context, signingIn: true, _ => { });
        }

        var page = await context.NewPageAsync();
        await MemberSession.SignInAsync(page, usernameVariable, passwordVariable);
        return await context.StorageStateAsync();
    }

    private static async Task GuardAsync(IBrowserContext context, CollectOptions options, Measurement result)
    {
        if (options.Live is { } live)
        {
            await live.GuardAsync(context, signingIn: false, reason => { lock (result) { result.Gaps.Add(reason); } });
        }
    }

    internal static async Task<T> ReadAsync<T>(IPage page, string script)
    {
        var json = await page.EvaluateAsync<string>($"() => JSON.stringify(({script})())");
        return JsonSerializer.Deserialize<T>(json, ReadOptions)!;
    }

    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class FrameWindow
    {
        public List<double> Frames { get; set; } = [];

        public int Loaf { get; set; }
    }

    private sealed class ObserverReading
    {
        public double Lcp { get; set; }

        public List<LayoutShiftEntry> Shifts { get; set; } = [];

        public double? LoadEnd { get; set; }

        public List<double> Events { get; set; } = [];

        public List<string> Supported { get; set; } = [];

        public List<double> Hover { get; set; } = [];
    }
}
