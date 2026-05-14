namespace KCC.Web.Features.Sections.BentoBox;

public class BentoBoxCell(int row, int column, int colSpan, int rowSpan)
{
    public int Row { get; } = row;
    public int Column { get; } = column;
    public int ColSpan { get; } = colSpan;
    public int RowSpan { get; } = rowSpan;

    public string ZoneName => $"r{Row}c{Column}";
}
