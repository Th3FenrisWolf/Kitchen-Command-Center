using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public static class RunnerCalibration
{
    private const int Samples = 5;

    private const string Workload = """
        () => {
          const start = performance.now();
          const root = document.createElement('div');
          document.body.append(root);
          for (let index = 0; index < 18000; index++) {
            const item = document.createElement('div');
            item.className = `item item-${index % 7}`;
            item.textContent = `Item ${index}`;
            root.append(item);
          }
          let checksum = root.offsetHeight;
          const numbers = Array.from({ length: 360000 }, (_, index) => (index * 2654435761) % 1000003);
          numbers.sort((left, right) => left - right);
          checksum += JSON.parse(JSON.stringify(numbers.slice(0, 120000))).length;
          root.remove();
          return checksum > 0 ? performance.now() - start : -1;
        }
        """;

    public static async Task<IReadOnlyList<double>> SampleAsync(IBrowser browser)
    {
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<!doctype html><title>calibration</title><body></body>");
        _ = await page.EvaluateAsync<double>(Workload);
        var samples = new List<double>();
        for (var index = 0; index < Samples; index++)
        {
            samples.Add(Math.Round(await page.EvaluateAsync<double>(Workload), 1));
        }

        return samples;
    }
}
