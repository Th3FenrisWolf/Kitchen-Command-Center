namespace KCC.Web.Features.Models.Common;

public record SsrResult
{
    public string Html { get; set; }

    // Development only: the SSR service inlines the rendered app's compiled style-block CSS,
    // which has no extracted stylesheet to link before the client bundle runs.
    public string Css { get; set; }
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
    public string Css { get; set; }
    public int RenderTime { get; set; }
}

public class SsrErrorResponse
{
    public string Error { get; set; }
    public string Stack { get; set; }
}
