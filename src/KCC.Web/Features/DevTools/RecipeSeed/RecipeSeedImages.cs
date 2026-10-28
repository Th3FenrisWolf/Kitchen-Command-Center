using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace KCC.Web.Features.DevTools.RecipeSeed;

// Stands in for a food photo, so pages decode a photo-sized file without a licensed picture in the repository.
// Grain over every pixel keeps the JPEG near a real photo's size; a flat gradient would compress to almost nothing.
public static class RecipeSeedImages
{
    public const int Width = 1200;
    public const int Height = 900;

    private const int Quality = 82;
    private const int Grain = 28;

    public static byte[] Jpeg(string recipeName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"photo::{recipeName}"));
        var hue = (hash[0] | (hash[1] << 8)) % 360;
        var light = FromHsl(hue, 0.55, 0.72);
        var dark = FromHsl((hue + 25) % 360, 0.6, 0.32);
        var plates = Enumerable.Range(0, 3)
            .Select(index => (X: hash[2 + index] / 255d * Width, Y: hash[5 + index] / 255d * Height, Radius: 120 + hash[8 + index]))
            .ToArray();
        var random = new Random(BitConverter.ToInt32(hash, 12));

        using var image = new Image<Rgb24>(Width, Height);
        image.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (var x = 0; x < Width; x++)
                {
                    var t = (((double)x / Width) + ((double)y / Height)) / 2;
                    var shine = plates.Max(plate => Math.Max(0, 1 - (Distance(x, y, plate.X, plate.Y) / plate.Radius)));
                    var mix = Math.Clamp(t - (shine * 0.6), 0, 1);
                    var grain = random.Next(-Grain, Grain + 1);
                    row[x] = new Rgb24(
                        Channel(light.R, dark.R, mix, grain),
                        Channel(light.G, dark.G, mix, grain),
                        Channel(light.B, dark.B, mix, grain));
                }
            }
        });

        using var output = new MemoryStream();
        image.SaveAsJpeg(output, new JpegEncoder { Quality = Quality });
        return output.ToArray();
    }

    private static double Distance(int x, int y, double toX, double toY) => Math.Sqrt(((x - toX) * (x - toX)) + ((y - toY) * (y - toY)));

    private static byte Channel(byte from, byte to, double mix, int grain) =>
        (byte)Math.Clamp((int)Math.Round(from + ((to - from) * mix)) + grain, 0, 255);

    private static Rgb24 FromHsl(int hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        var sector = hue / 60d;
        var second = chroma * (1 - Math.Abs((sector % 2) - 1));
        var (r, g, b) = (int)sector switch
        {
            0 => (chroma, second, 0d),
            1 => (second, chroma, 0d),
            2 => (0d, chroma, second),
            3 => (0d, second, chroma),
            4 => (second, 0d, chroma),
            _ => (chroma, 0d, second),
        };
        var offset = lightness - (chroma / 2);
        return new Rgb24(ToByte(r + offset), ToByte(g + offset), ToByte(b + offset));
    }

    private static byte ToByte(double channel) => (byte)Math.Round(channel * 255);
}
