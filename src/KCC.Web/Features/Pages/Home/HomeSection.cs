namespace KCC.Web.Features.Pages.Home;

public abstract record HomeSection(string Alias, string Classes);

public sealed record RichTextSection(Guid Key, string Classes, int Tear, string Html) : HomeSection("richTextBlock", Classes);

public sealed record CardGridSection(string Classes, string Heading, int Columns, IReadOnlyList<HomeCard> Cards)
    : HomeSection("cardGridBlock", Classes);

public sealed record StackerSection(Guid Key, string Classes, int Tear, string Heading, string BodyHtml, HomeImage Image, IReadOnlyList<StackerCard> Cards)
    : HomeSection("stackerBlock", Classes)
{
    public bool HasSheet => !string.IsNullOrWhiteSpace(BodyHtml) || Image is not null;
}

public sealed record HomeCard(Guid Key, string Heading, string SubHeading, string Body, string BackgroundColor, HomeLink Link, int Tear)
{
    public bool HasDrawer => !string.IsNullOrWhiteSpace(Body) || Link is not null;
}

public sealed record HomeLink(string Text, string Url, string Target);

public sealed record HomeImage(string Url, string Alt);

// Serialised straight into the Stacker component's `cards` prop, so the names follow its StackerCard interface.
public sealed record StackerCard(Guid Key, string Heading, string SubHeading, string BackgroundColor, int Tear);
