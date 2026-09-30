using KCC.Web.Features.Models.Generated;

namespace KCC.Web.Features.Pages.Home;

// Tailwind never scans .cs files: every class built here is in the safelist in Styles/TailwindConfig.css.
public static class SectionStyle
{
    public static string Classes(SectionSettings settings) => Classes(settings?.Background, settings?.Width);

    public static string Classes(string background, string width)
    {
        var fill = Fill(background);
        var filled = fill.Length > 0;
        return string.Join(' ', new[] { Width(width, filled), fill, filled ? "p-6 lg:p-12" : string.Empty }.Where(name => name.Length > 0));
    }

    public static string Wash(string wash) => string.IsNullOrWhiteSpace(wash) ? string.Empty : $"bg-{wash.ToLowerInvariant()}";

    // A full-width section has no column gutter of its own; a filled one takes its padding instead.
    private static string Width(string width, bool filled) => width switch
    {
        "Thin" => "thin",
        "Breakout" => "breakout",
        "Full width" => filled ? "full-width" : "full-width px-4",
        _ => string.Empty,
    };

    // A section on the desk stays unfilled: an opaque desk fill would cover the grain that sits under the page.
    private static string Fill(string background) => background switch
    {
        "Desk two" => "bg-desk-2",
        "Paper" => "bg-paper",
        "Paper two" => "bg-paper-2",
        _ => string.Empty,
    };
}
