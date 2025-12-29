using KitchenCommandCenter.Web.Features.Sections.Base;

namespace KitchenCommandCenter.Web.Features.Sections.MultipleColumn;

public class MultipleColumnSectionViewModel : BaseSectionViewModel
{
    public int ColumnCount { get; set; }
    public string SectionClass { get; set; }
    public string ContentAlignment { get; set; }
}
