using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public sealed record InteractionReading(int Restyled, int WashInvalidations);

public static partial class InteractionTrace
{
    // A running transition restyles its elements every frame, and how many frames fall in the window varies from run
    // to run, so a recalc only counts when something other than an animation invalidated style since the last one.
    // This Chromium emits no paint invalidation tracking events; a Paint event per repainted wash layer stands in.
    public static InteractionReading Summarize(IEnumerable<JsonElement> events)
    {
        var ordered = events.OrderBy(traceEvent => Number(traceEvent, "ts")).ToList();
        var recalcs = new List<(double Start, double End, int Count)>();
        var open = new Dictionary<string, double>();
        var invalidations = new List<(double At, bool Animation)>();
        var washes = 0;
        foreach (var traceEvent in ordered)
        {
            var name = traceEvent.TryGetProperty("name", out var n) ? n.GetString() : null;
            var phase = traceEvent.TryGetProperty("ph", out var ph) ? ph.GetString() : null;
            var at = Number(traceEvent, "ts");
            var args = traceEvent.TryGetProperty("args", out var a) && a.ValueKind == JsonValueKind.Object ? a : default;
            var data = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object ? d : default;
            switch (name)
            {
                case "UpdateLayoutTree" when phase == "X":
                    recalcs.Add((at, at + Number(traceEvent, "dur"), ElementCount(args)));
                    break;
                case "UpdateLayoutTree" when phase == "B":
                    open[$"{Number(traceEvent, "pid")}:{Number(traceEvent, "tid")}"] = at;
                    break;
                case "UpdateLayoutTree" when phase == "E":
                    var thread = $"{Number(traceEvent, "pid")}:{Number(traceEvent, "tid")}";
                    recalcs.Add((open.Remove(thread, out var begun) ? begun : at, at, ElementCount(args)));
                    break;
                case "StyleRecalcInvalidationTracking" or "StyleInvalidatorInvalidationTracking" or "ScheduleStyleInvalidationTracking":
                    invalidations.Add((at, data.ValueKind == JsonValueKind.Object && data.TryGetProperty("reason", out var reason) && reason.GetString() == "Animation"));
                    break;
                case "Paint" when data.ValueKind == JsonValueKind.Object
                    && data.TryGetProperty("nodeName", out var node)
                    && node.GetString() is { } nodeName
                    && WashClass().IsMatch(nodeName):
                    washes++;
                    break;
            }
        }

        var restyled = 0;
        var previous = double.NegativeInfinity;
        foreach (var (_, end, count) in recalcs.OrderBy(recalc => recalc.End))
        {
            if (invalidations.Any(item => item.At > previous && item.At <= end && !item.Animation))
            {
                restyled += count;
            }

            previous = end;
        }

        return new(restyled, washes);
    }

    private static int ElementCount(JsonElement args) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty("elementCount", out var count) && count.ValueKind == JsonValueKind.Number ? count.GetInt32() : 0;

    private static double Number(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;

    [GeneratedRegex(@"class='[^']*(?<![\w-])kcc-wash(?![\w-])")]
    private static partial Regex WashClass();
}

public static class TraceProbes
{
    public const int TuckBelowPx = 150;

    private const string Categories = "devtools.timeline,disabled-by-default-devtools.timeline.invalidationTracking";

    private const string Lift = ".kcc-slip--lift";

    private static readonly string[] Metrics = ["restyled_on_tuck", "restyled_on_hover", "wash_raster_on_interaction"];

    public static async Task<Measurement> ReadAsync(CaseSession session)
    {
        var result = new Measurement();
        if (session.DevTools is not { } devTools)
        {
            foreach (var metric in Metrics)
            {
                result.Gap(metric, "WebKit has no CDP trace");
            }

            return result;
        }

        var page = session.Page;
        await page.Mouse.MoveAsync(1, 1);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await RestAsync(page);
        var tuck = await TraceAsync(devTools, page, () => page.EvaluateAsync($"() => window.scrollTo(0, {TuckBelowPx * 2})"));
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await RestAsync(page);

        InteractionReading? hover = null;
        var hoverGap = "no .kcc-slip--lift on the page";
        var card = page.Locator(Lift).First;
        if (await page.Locator(Lift).CountAsync() > 0)
        {
            await card.ScrollIntoViewIfNeededAsync();
            await RestAsync(page);
            if (await card.BoundingBoxAsync() is { } box)
            {
                hover = await TraceAsync(devTools, page, () => page.Mouse.MoveAsync((float)(box.X + (box.Width / 2)), (float)(box.Y + (box.Height / 2))));
            }
            else
            {
                hoverGap = "the first .kcc-slip--lift has no bounding box";
            }

            await page.Mouse.MoveAsync(1, 1);
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await RestAsync(page);
        }

        return Record(tuck, hover, hoverGap);
    }

    public static Measurement Record(InteractionReading tuck, InteractionReading? hover, string? hoverGap)
    {
        var result = new Measurement();
        result.Values["restyled_on_tuck"] = tuck.Restyled;
        if (hover is { } hovered)
        {
            result.Values["restyled_on_hover"] = hovered.Restyled;
        }
        else
        {
            result.Gap("restyled_on_hover", hoverGap ?? "the hover was not traced");
        }

        result.Values["wash_raster_on_interaction"] = tuck.WashInvalidations + (hover?.WashInvalidations ?? 0);
        return result;
    }

    private static async Task RestAsync(IPage page)
    {
        await page.EvaluateAsync("() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        await page.WaitForTimeoutAsync(800);
    }

    private static async Task<InteractionReading> TraceAsync(ICDPSession devTools, IPage page, Func<Task> move)
    {
        var events = new List<JsonElement>();
        var done = new TaskCompletionSource();
        void Collect(object? sender, JsonElement? args)
        {
            if (args is { } data && data.TryGetProperty("value", out var batch))
            {
                lock (events)
                {
                    events.AddRange(batch.EnumerateArray().Select(item => item.Clone()));
                }
            }
        }

        void Complete(object? sender, JsonElement? args) => done.TrySetResult();

        var collected = devTools.Event("Tracing.dataCollected");
        var completed = devTools.Event("Tracing.tracingComplete");
        collected.OnEvent += Collect;
        completed.OnEvent += Complete;
        try
        {
            _ = await devTools.SendAsync("Tracing.start", new Dictionary<string, object>
            {
                ["categories"] = Categories,
                ["transferMode"] = "ReportEvents",
            });
            await move();
            await RestAsync(page);
            _ = await devTools.SendAsync("Tracing.end");
            await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            collected.OnEvent -= Collect;
            completed.OnEvent -= Complete;
        }

        lock (events)
        {
            return InteractionTrace.Summarize(events);
        }
    }
}
