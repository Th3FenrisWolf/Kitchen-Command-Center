using System;

namespace KitchenCommandCenter.Web.Features.Sections.Base;

public class BaseSectionViewModel
{
    public Guid SectionGuid { get; } = Guid.NewGuid();
    public string BackgroundColor { get; set; }
    public string ContentWidth { get; set; }
    public int PaddingBottom { get; set; }
    public int PaddingTop { get; set; }
}
