using System;

namespace KitchenCommandCenter.Web.Features.Tailwind;

public enum TailwindColor
{
    None = 0,
    Onyx,
    Bone,
    Crust,
    Surface,
    Mantle,
    Base,
    Rosewater,
    Flamingo,
    Pink,
    Mauve,
    Red,
    Maroon,
    Peach,
    Yellow,
    Green,
    Teal,
    Sky,
    Sapphire,
    Blue,
    Lavender,
}

public enum TailwindShade
{
    None = 0,
    Fifty = 50,
    OneHundred = 100,
    TwoHundred = 200,
    ThreeHundred = 300,
    FourHundred = 400,
    FiveHundred = 500,
    SixHundred = 600,
    SevenHundred = 700,
    EightHundred = 800,
    NineHundred = 900,
}

[AttributeUsage(AttributeTargets.Field)]
public class TailwindStyleAttribute(string tailwindStyle = "") : Attribute
{
    private readonly string tailwindStyle = tailwindStyle;

    public virtual string GetTailwindStyle() => tailwindStyle;
}

public abstract class TailwindColorAttribute(
    TailwindColor color,
    TailwindShade shade = TailwindShade.None
) : TailwindStyleAttribute
{
    private readonly TailwindColor color = color;
    private readonly TailwindShade shade = shade;

    public override string GetTailwindStyle() =>
        shade != TailwindShade.None
            ? $"{color.ToString().ToLowerInvariant()}-{(int)shade}"
            : $"{color.ToString().ToLowerInvariant()}";
}

public class TailwindBackgroundColorAttribute(
    TailwindColor color,
    TailwindShade shade = TailwindShade.None
) : TailwindColorAttribute(color, shade)
{
    public override string GetTailwindStyle() => $"bg-{base.GetTailwindStyle()}";
}

public class TailwindTextColorAttribute(
    TailwindColor color,
    TailwindShade shade = TailwindShade.None
) : TailwindColorAttribute(color, shade)
{
    public override string GetTailwindStyle() => $"text-{base.GetTailwindStyle()}";
}
