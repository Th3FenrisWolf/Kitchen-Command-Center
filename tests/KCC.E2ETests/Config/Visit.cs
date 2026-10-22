using System.Globalization;
using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class Visit
{
    public const string Cookie = "kcc-visit";

    public const double MaxElapsedSeconds = 300;

    public static async Task<Begun> BeginAsync(IBrowserContext context, Uri baseUrl, int secondsAgo, uint? seed = null)
    {
        var written = DateTimeOffset.UtcNow;
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = Cookie,
                Value = Value(seed ?? (uint)Random.Shared.NextInt64(0, 1L << 32), written.AddSeconds(-secondsAgo)),
                Domain = baseUrl.Host,
                Path = "/",
                SameSite = SameSiteAttribute.Lax,
            },
        ]);
        return new Begun(secondsAgo, written);
    }

    public static string Value(uint seed, DateTimeOffset startedAt) =>
        string.Create(CultureInfo.InvariantCulture, $"1.{seed:x8}.{startedAt.ToUnixTimeSeconds()}");

    public static async Task<string?> ReadAsync(IBrowserContext context) =>
        (await context.CookiesAsync()).FirstOrDefault(cookie => cookie.Name == Cookie)?.Value;

    public static uint SeedOf(string value) => uint.Parse(value.Split('.')[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    // The cookie floors the start to a whole second, which adds up to a second, and the page writes the elapsed
    // time rounded to a tenth.
    public sealed record Begun(int SecondsAgo, DateTimeOffset Written)
    {
        public (double Min, double Max) ElapsedWindow()
        {
            var gap = (DateTimeOffset.UtcNow - Written).TotalSeconds;
            return (Math.Min(SecondsAgo - 0.05, MaxElapsedSeconds), Math.Min(SecondsAgo + gap + 1.05, MaxElapsedSeconds));
        }
    }
}
