namespace KCC.Web.Models.Common;

public record SsrResult
{
    public string Html { get; set; }
    public string HeaderContent { get; set; }
    public string BodyContent { get; set; }
    public string FooterContent { get; set; }
}

public class SsrRequest
{
    public string HeaderContent { get; set; }
    public string BodyContent { get; set; }
    public string FooterContent { get; set; }
}

public class SsrResponse
{
    public string Html { get; set; }
    public int RenderTime { get; set; }
}
