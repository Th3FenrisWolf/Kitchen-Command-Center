using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using KitchenCommandCenter.Web.Extensions;
using KitchenCommandCenter.Web.Features.Providers;
using KitchenCommandCenter.Web.Features.Tailwind;
using KitchenCommandCenter.Web.Features.Widgets.Base;
using KitchenCommandCenter.Web.Features.Widgets.Button;
using KitchenCommandCenter.Web.Models.Constants;

[assembly: RegisterWidget(
    identifier: ButtonWidgetProperties.IDENTIFIER,
    name: "Button",
    propertiesType: typeof(ButtonWidgetProperties),
    customViewName: "~/Features/Widgets/Button/_ButtonWidget.cshtml",
    IconClass = "icon-arrow-right-circle",
    AllowCache = true
)]

namespace KitchenCommandCenter.Web.Features.Widgets.Button;

public enum ButtonAlignmentOptions
{
    [TailwindStyle("justify-start")]
    Left,

    [TailwindStyle("justify-center")]
    Center,

    [TailwindStyle("justify-end")]
    Right,
}

public class ButtonWidgetProperties : BaseWidgetProperties, IWidgetProperties
{
    public const string IDENTIFIER = "KitchenCommandCenter.Web.Button";

    public string ButtonText { get; set; } = WidgetConstants.ConfigHeading;

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 0,
        Label = "Variant",
        Options = "Primary\nSecondary\nOutlined\nTertiary"
    )]
    public string Variant { get; set; } = "Primary";

    [RequiredValidationRule]
    [RadioGroupComponent(Order = 10, Label = "Size", Options = "Large\nSmall")]
    public string Size { get; set; } = "Large";

    [TextInputComponent(
        Order = 20,
        Label = "URL",
        ExplanationText = "To link to a page in the current site, enter the relative path (i.e. /about). To link to an external page, enter the full URL (e.g. https://google.com)"
    )]
    public string ButtonUrl { get; set; }

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 30,
        Label = "Click Action",
        Options = "_self;Same Tab\n_blank;New Tab"
    )]
    public string ButtonTarget { get; set; } = "_self";

    [RequiredValidationRule]
    [DropDownComponent(
        Order = 40,
        Label = "Alignment",
        DataProviderType = typeof(EnumDropDownOptionsProvider<ButtonAlignmentOptions>)
    )]
    public string Alignment { get; set; } = ButtonAlignmentOptions.Center.GetTailwindStyle();
}
