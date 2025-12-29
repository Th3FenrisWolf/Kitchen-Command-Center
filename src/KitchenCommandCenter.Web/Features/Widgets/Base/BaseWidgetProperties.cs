using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace KitchenCommandCenter.Web.Features.Widgets.Base;

[FormCategory(Label = "Margin", Order = 100, Collapsible = true, IsCollapsed = true)]
public class BaseWidgetProperties : IWidgetProperties
{
    private const string MarginOptions = "0;None\r\n4;Small\r\n8;Medium\r\n12;Large";

    [RequiredValidationRule]
    [DropDownComponent(Order = 101, Label = "Margin Top", Options = MarginOptions)]
    public string MarginTop { get; set; } = "0";

    [RequiredValidationRule]
    [DropDownComponent(Order = 102, Label = "Margin Bottom", Options = MarginOptions)]
    public string MarginBottom { get; set; } = "0";

    public string GetMarginClasses() => $"mt-{MarginTop} mb-{MarginBottom}";
}
