using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Error;

public class ErrorViewModel : BasePageViewModel
{
    public int StatusCode { get; set; }

    public string Heading { get; set; }

    public string Body { get; set; }

    public static ErrorViewModel For(int statusCode, string heading, string body)
    {
        var resolvedHeading = string.IsNullOrWhiteSpace(heading) ? "Error" : heading;
        return new ErrorViewModel
        {
            StatusCode = statusCode,
            Heading = resolvedHeading,
            Title = resolvedHeading,
            Body = string.IsNullOrWhiteSpace(body) ? "An unexpected error occurred." : body,
        };
    }
}
