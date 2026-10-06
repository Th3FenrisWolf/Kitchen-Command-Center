namespace KCC.Web.Features.Components.Header;

public class HeaderViewModel
{
    // Resolved server-side because the header renders on every page, while each page controller assembles its
    // own resource strings.
    public string LogoAlt { get; set; }

    public NavModel Nav { get; set; }
}
