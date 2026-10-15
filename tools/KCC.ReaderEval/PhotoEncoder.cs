using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KCC.ReaderEval;

public static class PhotoEncoder
{
    public const long MaxPhotoBytes = 2_200_000;

    private static readonly int[] Qualities = [85, 70, 55];

    public static PhotoEncoding Encode(byte[] original, int edge)
    {
        Image image;
        try
        {
            image = Image.Load(original);
        }
        catch (Exception exception) when (exception is ImageFormatException or NotSupportedException)
        {
            return new PhotoEncoding(null, "cannot be decoded. Save it as a JPEG, PNG or WebP.");
        }

        using (image)
        {
            image.Mutate(context => context.AutoOrient());
            DropMetadata(image);
            var (width, height) = ScaledSize(image.Width, image.Height, edge);
            if (width != image.Width || height != image.Height)
            {
                image.Mutate(context => context.Resize(width, height, KnownResamplers.Bicubic));
            }

            foreach (var quality in Qualities)
            {
                var data = Jpeg(image, quality);
                if (data.LongLength <= MaxPhotoBytes)
                {
                    return new PhotoEncoding(new EncodedPhoto(data, width, height, quality), null);
                }
            }
        }

        return new PhotoEncoding(null, "is still over 2.2 MB as a JPEG at quality 55, so the page would refuse it.");
    }

    private static (int Width, int Height) ScaledSize(int width, int height, int edge)
    {
        var scale = Math.Min(1.0, edge / (double)Math.Max(width, height));
        return (
            (int)Math.Round(width * scale, MidpointRounding.AwayFromZero),
            (int)Math.Round(height * scale, MidpointRounding.AwayFromZero));
    }

    private static byte[] Jpeg(Image image, int quality)
    {
        using var stream = new MemoryStream();

        // A browser's canvas writes 4:2:0 whatever the source, where ImageSharp would keep a camera JPEG's own.
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = quality, ColorType = JpegEncodingColor.YCbCrRatio420 });
        return stream.ToArray();
    }

    private static void DropMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;
    }
}

public sealed record PhotoEncoding(EncodedPhoto Photo, string Refusal);

public sealed record EncodedPhoto(byte[] Data, int Width, int Height, int Quality);
