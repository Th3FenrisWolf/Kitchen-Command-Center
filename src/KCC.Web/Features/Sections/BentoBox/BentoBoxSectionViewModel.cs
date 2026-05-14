using KCC.Web.Features.Sections.Base;

namespace KCC.Web.Features.Sections.BentoBox;

public class BentoBoxSectionViewModel : BaseSectionViewModel
{
    public int Rows { get; set; } = 1;
    public int Columns { get; set; } = 1;
    public List<BentoBoxCell> Cells { get; set; } = [];
    public string GridLayoutJson { get; set; } = string.Empty;
    public bool IsEditMode { get; set; }
    public int PageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
}
