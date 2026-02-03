using System;

namespace KCC.Web.Features.Sections.Base;

public class BaseSectionViewModel
{
    public Guid SectionGuid { get; } = Guid.NewGuid();
    public string Heading { get; set; }
    public string BackgroundColor { get; set; }
    public string TextColor { get; set; }
    public string ContentWidth { get; set; }
    public int PaddingBottom { get; set; }
    public int PaddingTop { get; set; }
    public int HorizontalPadding { get; set; }
}
