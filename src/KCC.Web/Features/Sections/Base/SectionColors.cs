namespace KCC.Web.Features.Sections.Base;

public static class SectionColors
{
    // Editors do not pick text colours: every ground a section can take is contrast-tested with ink in
    // both ramps.
    public static string TextClassFor(string backgroundClass) => "text-ink";
}
