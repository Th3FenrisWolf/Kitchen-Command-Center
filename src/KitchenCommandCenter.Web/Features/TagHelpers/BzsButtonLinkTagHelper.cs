using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KitchenCommandCenter.Web.Features.TagHelpers;

public class BzsButtonLinkTagHelper : TagHelper
{
    public string Class { get; set; }
    public string Href { get; set; }
    public string Target { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "a";

        output.Attributes.SetAttribute("class", $"btn not-prose {Class}");
        output.Attributes.SetAttribute("href", Href);
        output.Attributes.SetAttribute("target", Target);
    }
}
