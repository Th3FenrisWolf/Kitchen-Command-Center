using KCC.Web.Features.Extensions;

namespace KCC.Web.Features.Sections.Base;

public static class SectionColors
{
    // Derived from the options rather than hand-listed, so the two cannot drift: every option that is not
    // one of the grounds is a wash.
    private static readonly HashSet<string> WashBackgrounds =
    [
        .. Enum.GetValues<SectionBackgroundColorOptions>()
            .Where(option => option is not (
                SectionBackgroundColorOptions.Desk
                or SectionBackgroundColorOptions.Paper
                or SectionBackgroundColorOptions.PaperTwo
            ))
            .Select(option => option.GetTailwindStyle()),
    ];

    // Washes take the fixed dark ink that reads on them in both ramps. Every other ground falls through to
    // ink, which contrast.test.ts pins against desk, desk-2, paper and paper-2 in both ramps — so a ground
    // this does not recognise (content still holding a retired token, an empty value, a role added to the
    // options later) stays legible instead of turning near-black on the dark desk. Editors no longer pick
    // text colours.
    public static string TextClassFor(string backgroundClass) =>
        WashBackgrounds.Contains(backgroundClass) ? "text-ink-on-wash" : "text-ink";
}
