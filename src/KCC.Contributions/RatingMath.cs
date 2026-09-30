using KCC.Contributions.Data;

namespace KCC.Contributions;

public static class RatingMath
{
    public static bool IsValidRating(decimal rating) =>
        rating >= 0.5m && rating <= 5m && (rating * 2m) % 1m == 0m;

    public static string ClampText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= ContributionsDbContext.MaxTextLength ? trimmed : trimmed[..ContributionsDbContext.MaxTextLength];
    }

    // Buckets run from 1★ at index 0 to 5★ at index 4, as the histogram draws them. A half star has not reached
    // the star above it, so it rounds down; a lone 0.5 has no 0★ bucket and counts as 1★.
    public static int[] Distribution(IEnumerable<decimal> ratings)
    {
        var buckets = new int[5];
        foreach (var rating in ratings)
        {
            var star = Math.Clamp((int)Math.Ceiling(rating - 0.5m), 1, 5);
            buckets[star - 1]++;
        }

        return buckets;
    }
}
