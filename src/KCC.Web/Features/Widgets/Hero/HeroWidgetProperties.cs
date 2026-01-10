using System.Collections.Generic;
using CMS.ContentEngine;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Tailwind;
using KCC.Web.Features.Widgets.Base;
using KCC.Web.Features.Widgets.Hero;
using KCC.Web.Models.Constants;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterWidget(
    identifier: HeroWidgetProperties.IDENTIFIER,
    name: "Hero",
    propertiesType: typeof(HeroWidgetProperties),
    customViewName: "~/Features/Widgets/Hero/_HeroWidget.cshtml",
    IconClass = "icon-tab",
    AllowCache = true
)]

namespace KCC.Web.Features.Widgets.Hero;

public enum HeroTextAlignmentOptions
{
    [TailwindStyle("text-start")]
    Left,

    [TailwindStyle("text-center")]
    Center,
}

public enum HeroBackgroundColorOptions
{
    [TailwindBackgroundColor(TailwindColor.Surface)]
    Surface,

    [TailwindBackgroundColor(TailwindColor.Surface, TailwindShade.OneHundred)]
    Surface100,

    [TailwindBackgroundColor(TailwindColor.Surface, TailwindShade.TwoHundred)]
    Surface200,
}

public class HeroWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    public const string IDENTIFIER = "KCC.Web.HeroWidget";

    public string Heading { get; set; } =
        $"<h1>{WidgetConstants.ConfigHeading}</h1><h2>{WidgetConstants.ConfigSubHeading}</h2>";

    [DropDownComponent(Order = 0, Label = "Layout", Options = "Split\nFull")]
    public string Layout { get; set; } = "Split";

    [DropDownComponent(
        Order = 10,
        Label = "Split Layout",
        Options = "textFirst;Text First\nimageFirst;Image First"
    )]
    [VisibleIfEqualTo(nameof(Layout), "Split")]
    public string SplitLayout { get; set; } = "textFirst";

    [DropDownComponent(
        Order = 10,
        Label = "Text Alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeroTextAlignmentOptions>)
    )]
    [VisibleIfEqualTo(nameof(Layout), "Full")]
    public string TextAlignment { get; set; } = "left";

    [DropDownComponent(Order = 20, Label = "Text Width", Options = "Full\nNarrow")]
    [VisibleIfEqualTo(nameof(Layout), "Full")]
    public string TextWidth { get; set; } = "Full";

    [DropDownComponent(
        Order = 30,
        Label = "Background Color",
        ExplanationText = "Used behind text for the Split Layout, or as the full background if an image isn't selected",
        DataProviderType = typeof(EnumDropDownOptionsProvider<HeroBackgroundColorOptions>)
    )]
    public string BackgroundColor { get; set; } = "lightGray";

    [ContentItemSelectorComponent(
        ImageItem.CONTENT_TYPE_NAME,
        Order = 40,
        Label = "Background Image",
        MaximumItems = 1
    )]
    public IEnumerable<ContentItemReference> BackgroundImage { get; set; }
}
