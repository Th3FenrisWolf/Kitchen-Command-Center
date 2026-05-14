#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KCC.Web.Features.Sections.BentoBox;

public class CellSpan
{
    [JsonPropertyName("colSpan")]
    public int ColSpan { get; set; } = 1;

    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; } = 1;
}

public class BentoBoxGridLayout
{
    private const int MinSize = 1;
    private const int MaxSize = 6;

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 1;

    [JsonPropertyName("columns")]
    public int Columns { get; set; } = 1;

    [JsonPropertyName("spans")]
    public Dictionary<string, CellSpan> Spans { get; set; } = new();

    public static BentoBoxGridLayout Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BentoBoxGridLayout();
        }

        try
        {
            var layout = JsonSerializer.Deserialize<BentoBoxGridLayout>(json);
            layout?.Validate();
            return layout ?? new BentoBoxGridLayout();
        }
        catch
        {
            return new BentoBoxGridLayout();
        }
    }

    public void Validate()
    {
        Rows = Math.Clamp(Rows, MinSize, MaxSize);
        Columns = Math.Clamp(Columns, MinSize, MaxSize);

        var validSpans = new Dictionary<string, CellSpan>();
        foreach (var (key, span) in Spans)
        {
            var (row, col) = ParseCellKey(key);
            if (row < 0 || row >= Rows || col < 0 || col >= Columns)
            {
                continue;
            }

            span.ColSpan = Math.Clamp(span.ColSpan, 1, Columns - col);
            span.RowSpan = Math.Clamp(span.RowSpan, 1, Rows - row);

            if (span.ColSpan > 1 || span.RowSpan > 1)
            {
                validSpans[key] = span;
            }
        }

        Spans = validSpans;
    }

    public List<BentoBoxCell> BuildCells()
    {
        var occupied = new bool[Rows, Columns];
        var cells = new List<BentoBoxCell>();

        foreach (var (key, span) in Spans)
        {
            var (row, col) = ParseCellKey(key);
            if (row < 0 || row >= Rows || col < 0 || col >= Columns)
            {
                continue;
            }

            for (int r = row; r < row + span.RowSpan && r < Rows; r++)
            {
                for (int c = col; c < col + span.ColSpan && c < Columns; c++)
                {
                    occupied[r, c] = true;
                }
            }
        }

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                var key = $"r{r}c{c}";
                if (Spans.TryGetValue(key, out var span))
                {
                    cells.Add(new BentoBoxCell(r, c, span.ColSpan, span.RowSpan));
                }
                else if (!occupied[r, c])
                {
                    cells.Add(new BentoBoxCell(r, c, 1, 1));
                }
            }
        }

        return cells;
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static (int row, int col) ParseCellKey(string key)
    {
        try
        {
            int rIndex = key.IndexOf('r');
            int cIndex = key.IndexOf('c');
            if (rIndex < 0 || cIndex < 0)
            {
                return (-1, -1);
            }

            int row = int.Parse(key.AsSpan(rIndex + 1, cIndex - rIndex - 1));
            int col = int.Parse(key.AsSpan(cIndex + 1));
            return (row, col);
        }
        catch
        {
            return (-1, -1);
        }
    }
}
