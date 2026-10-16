using System.Text.RegularExpressions;

namespace KCC.IntegrationTests.Config;

public static class Slips
{
    public const string Blank = "<span class=\"kcc-blank\" aria-hidden=\"true\"><span></span></span>";

    public static IReadOnlyList<string> In(string template)
    {
        var contents = new List<string>();
        foreach (Match open in Regex.Matches(template, "<(\\w+)\\b[^>]*\\bclass=\"kcc-slip\\b[^\"]*\"[^>]*>"))
        {
            var start = open.Index + open.Length;
            var depth = 1;
            foreach (Match tag in new Regex($"<(/?){open.Groups[1].Value}\\b[^>]*>").Matches(template, start))
            {
                depth += tag.Groups[1].Value.Length > 0 ? -1 : 1;
                if (depth == 0)
                {
                    contents.Add(template[start..tag.Index]);
                    break;
                }
            }
        }

        return contents;
    }

    public static bool EndsWithOneBlank(string slip) =>
        slip.TrimEnd().EndsWith(Blank, StringComparison.Ordinal) && Regex.Count(slip, Regex.Escape(Blank)) == 1;
}
