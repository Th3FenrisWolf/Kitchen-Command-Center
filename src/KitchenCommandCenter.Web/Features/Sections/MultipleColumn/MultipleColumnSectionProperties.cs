using System;
using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Extensions;
using KitchenCommandCenter.Web.Features.Providers;
using KitchenCommandCenter.Web.Features.Sections.Base;
using KitchenCommandCenter.Web.Features.Tailwind;

namespace KitchenCommandCenter.Web.Features.Sections.MultipleColumn;

public enum ColumnLayout
{
    OneColumn,
    TwoColumns,
    ThreeColumns,
    FourColumns,
    LeftTwoThirds,
    RightTwoThirds,
}

public enum ContentVerticalAlignmentOptions
{
    [TailwindStyle("items-start")]
    Top,

    [TailwindStyle("items-center")]
    Middle,

    [TailwindStyle("items-end")]
    Bottom,
}

public class MultipleColumnSectionProperties : BaseSectionProperties
{
    [RequiredValidationRule]
    [DropDownComponent(
        Order = 10,
        Label = "Column Style",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ColumnLayout>)
    )]
    public string ColumnStyle { get; set; } = ColumnLayout.OneColumn.ToString();

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 20,
        Label = "Content Vertical Alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ContentVerticalAlignmentOptions>)
    )]
    public string ContentAlignment { get; set; } =
        ContentVerticalAlignmentOptions.Top.GetTailwindStyle();

    public (string, int) GetColumnStyle() => GetColumnStyleProperties(
        Enum.TryParse<ColumnLayout>(ColumnStyle, out var result)
            ? result
            : ColumnLayout.OneColumn);

    private (string ColumnStyleClass, int ColumnCount) GetColumnStyleProperties(
        ColumnLayout columnStyle
    ) =>
        columnStyle switch
        {
            ColumnLayout.OneColumn => ("one-column", 1),
            ColumnLayout.TwoColumns => ("two-columns", 2),
            ColumnLayout.ThreeColumns => ("three-columns", 3),
            ColumnLayout.FourColumns => ("four-columns", 4),
            ColumnLayout.LeftTwoThirds => ("left-two-thirds", 2),
            ColumnLayout.RightTwoThirds => ("right-two-thirds", 2),
            _ => ("even-columns", 1),
        };
}
