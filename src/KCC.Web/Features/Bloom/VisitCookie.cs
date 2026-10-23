using System.Globalization;

namespace KCC.Web.Features.Bloom;

public static class VisitCookie
{
    public const string Name = "kcc-visit";

    private const string Version = "1";

    private static readonly TimeSpan OldestStart = TimeSpan.FromDays(365);

    public static string Format(Visit visit) =>
        string.Create(CultureInfo.InvariantCulture, $"{Version}.{visit.Seed:x8}.{visit.StartedAt.ToUnixTimeSeconds()}");

    public static Visit Parse(string value, DateTimeOffset now)
    {
        var parts = value?.Split('.');
        if (parts is not [Version, var seedHex, var startText]
            || seedHex.Length != 8
            || !seedHex.All(char.IsAsciiHexDigitLower)
            || !startText.All(char.IsAsciiDigit)
            || !uint.TryParse(seedHex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var seed)
            || !long.TryParse(startText, NumberStyles.None, CultureInfo.InvariantCulture, out var startSeconds)
            || startSeconds > now.ToUnixTimeSeconds()
            || startSeconds < (now - OldestStart).ToUnixTimeSeconds())
        {
            return null;
        }

        return new Visit(seed, DateTimeOffset.FromUnixTimeSeconds(startSeconds), now);
    }
}
