using System;
using KCC.Web.Extensions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Tailwind;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KCC.Web.Features.Sections.Base;

public enum Spacing
{
    None,
    Small,
    Medium,
    Large,
}

public enum SectionBackgroundColorOptions
{
    [TailwindBackgroundColor(TailwindColor.Bone)]
    Bone,
    [TailwindBackgroundColor(TailwindColor.Base)]
    Base,
    [TailwindBackgroundColor(TailwindColor.Crust)]
    Crust,
    [TailwindBackgroundColor(TailwindColor.Surface)]
    Surface,
    [TailwindBackgroundColor(TailwindColor.Mantle)]
    Mantle,
}

public enum SectionTextColorOptions
{
    [TailwindTextColor(TailwindColor.Bone)]
    Bone,
    [TailwindTextColor(TailwindColor.Onyx)]
    Onyx,
}

public class BaseSectionProperties : ISectionProperties
{
    [TextInputComponent(
        Order = 0,
        Label = "Heading"
    )]
    public string Heading { get; set; }

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 1,
        Label = "Background Color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<SectionBackgroundColorOptions>)
    )]
    public string BackgroundColor { get; set; } =
        SectionBackgroundColorOptions.Bone.GetTailwindStyle();

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 2,
        Label = "Text Color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<SectionTextColorOptions>)
    )]
    public string TextColor { get; set; } = SectionTextColorOptions.Onyx.GetTailwindStyle();

    [RequiredValidationRule]
    [DropDownComponent(Order = 3, Label = "Content Width", Options = "Thin\nContainer\nBreakout\nFull Width")]
    public string ContentWidth { get; set; } = "Container";

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 4,
        Label = "Padding Top",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Spacing>)
    )]
    public string PaddingTop { get; set; } = Spacing.Medium.ToString();

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 5,
        Label = "Padding Bottom",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Spacing>)
    )]
    public string PaddingBottom { get; set; } = Spacing.Medium.ToString();

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 6,
        Label = "Horizontal Padding",
        DataProviderType = typeof(EnumDropDownOptionsProvider<Spacing>)
    )]
    public string HorizontalPadding { get; set; } = Spacing.None.ToString();

    public int GetPaddingTop() => GetSpacing(
        Enum.TryParse<Spacing>(PaddingTop, out var result)
            ? result
            : Spacing.Medium);

    public int GetPaddingBottom() => GetSpacing(
        Enum.TryParse<Spacing>(PaddingBottom, out var result)
            ? result
            : Spacing.Medium);

    public int GetHorizontalPadding() => GetSpacing(
        Enum.TryParse<Spacing>(HorizontalPadding, out var result)
            ? result
            : Spacing.None);

    public static int GetSpacing(Spacing spacing) => spacing switch
    {
        Spacing.Small => 4,
        Spacing.Medium => 8,
        Spacing.Large => 12,
        Spacing.None or _ => 0,
    };
}
