using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class GridRows
{
    // Each slip is rotated, so its bounding box grows with the tilt. offsetTop and offsetHeight ignore transforms.
    private const string SheetHeightsByRow = """
        grid => {
          const rows = new Map()
          for (const slip of grid.querySelectorAll(':scope > .kcc-slip')) {
            const heights = rows.get(slip.offsetTop) ?? []
            heights.push(slip.querySelector('.kcc-sheet').offsetHeight)
            rows.set(slip.offsetTop, heights)
          }
          return [...rows.values()]
        }
        """;

    public static Task<double[][]> SheetHeightsAsync(ILocator grid) => grid.EvaluateAsync<double[][]>(SheetHeightsByRow);
}
