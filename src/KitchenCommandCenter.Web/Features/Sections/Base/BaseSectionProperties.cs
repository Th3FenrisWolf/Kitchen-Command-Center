using System;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Extensions;
using KitchenCommandCenter.Web.Features.Providers;
using KitchenCommandCenter.Web.Features.Tailwind;

namespace KitchenCommandCenter.Web.Features.Sections.Base;

public enum VerticalSpacing
{
    None,
    Small,
    Medium,
    Large,
}

public enum SectionBackgroundColorOptions
{
    [TailwindBackgroundColor(TailwindColor.White)]
    White,
}

public class BaseSectionProperties : ISectionProperties
{
    [RequiredValidationRule]
    [DropDownComponent(
        Order = 0,
        Label = "Background Color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<SectionBackgroundColorOptions>)
    )]
    public string BackgroundColor { get; set; } =
        SectionBackgroundColorOptions.White.GetTailwindStyle();

    [RequiredValidationRule]
    [DropDownComponent(Order = 30, Label = "Content Width", Options = "Container\nFull Width")]
    public string ContentWidth { get; set; } = "Container";

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 40,
        Label = "Padding Top",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalSpacing>)
    )]
    public string PaddingTop { get; set; } = VerticalSpacing.Large.ToString();

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 50,
        Label = "Padding Bottom",
        DataProviderType = typeof(EnumDropDownOptionsProvider<VerticalSpacing>)
    )]
    public string PaddingBottom { get; set; } = VerticalSpacing.Large.ToString();

    public int GetPaddingTop() => GetVerticalSpacing(
        Enum.TryParse<VerticalSpacing>(PaddingTop, out var result)
            ? result
            : VerticalSpacing.Large);

    public int GetPaddingBottom() => GetVerticalSpacing(
        Enum.TryParse<VerticalSpacing>(PaddingBottom, out var result)
            ? result
            : VerticalSpacing.Large);

    public static int GetVerticalSpacing(VerticalSpacing verticalSpacing) => verticalSpacing switch
    {
        VerticalSpacing.Small => 4,
        VerticalSpacing.Medium => 8,
        VerticalSpacing.Large => 12,
        VerticalSpacing.None or _ => 0,
    };
}
