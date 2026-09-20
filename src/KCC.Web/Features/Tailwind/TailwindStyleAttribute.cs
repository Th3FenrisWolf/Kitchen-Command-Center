namespace KCC.Web.Features.Tailwind;

public enum TailwindColor
{
    Desk,
    DeskTwo,
    Paper,
    PaperTwo,
    Ink,
    InkSoft,
    Marker,
    MarkerInk,

    Peach,
    Yellow,
    Green,
    Teal,
    Sky,
    Lavender,
    Pink,
    Red,
}

public static class TailwindColorExtensions
{
    // Enum names cannot spell hyphens or digits, so the multi-word roles map by hand.
    public static string ToToken(this TailwindColor color) => color switch
    {
        TailwindColor.DeskTwo => "desk-2",
        TailwindColor.PaperTwo => "paper-2",
        TailwindColor.InkSoft => "ink-soft",
        TailwindColor.MarkerInk => "marker-ink",
        _ => color.ToString().ToLowerInvariant(),
    };
}

[AttributeUsage(AttributeTargets.Field)]
public class TailwindStyleAttribute(string tailwindStyle = "") : Attribute
{
    private readonly string tailwindStyle = tailwindStyle;

    public virtual string GetTailwindStyle() => tailwindStyle;
}

public class TailwindBackgroundColorAttribute(TailwindColor color) : TailwindStyleAttribute
{
    public override string GetTailwindStyle() => $"bg-{color.ToToken()}";
}
