namespace KCC.Web.Features.Models.Common;

public record SsrResult
{
    public string Html { get; set; }
    public string HeaderContent { get; set; }
    public string BodyContent { get; set; }
    public string FooterContent { get; set; }
    public bool IsPreview { get; set; }

    // Populated only in Development when the SSR service reports a render
    // exception; SsrHtmlContent re-emits it for the dev error overlay.
    public string ErrorMessage { get; set; }
    public string ErrorStack { get; set; }
}

public class SsrResponse
{
    public string Html { get; set; }
    public int RenderTime { get; set; }
}

public class SsrErrorResponse
{
    public string Error { get; set; }
    public string Stack { get; set; }
}
