using System.Runtime.InteropServices;
using KCC.ReaderEval;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace KCC.UnitTests.Tools;

public class ReaderEvalEncoderTests
{
    [Test]
    public async Task ThePhotoLimit_Is2Point2MillionBytes()
    {
        var limit = PhotoEncoder.MaxPhotoBytes;

        _ = await Assert.That(limit).IsEqualTo(2_200_000L);
    }

    [Test]
    public async Task TheExifOrientation_IsApplied_AndTheJpegCarriesNoExif()
    {
        var sideways = new Image<Rgb24>(300, 200, new Rgb24(255, 255, 255));
        Paint(sideways, new Rgb24(255, 0, 0), width: 60, height: 40);
        sideways.Metadata.ExifProfile = new ExifProfile();
        sideways.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);

        var photo = PhotoEncoder.Encode(Saved(sideways, new JpegEncoder { Quality = 95 }), edge: 2000).Photo;

        using var upright = Image.Load<Rgb24>(photo.Data);
        _ = await Assert.That((upright.Width, upright.Height)).IsEqualTo((200, 300));
        _ = await Assert.That(IsRed(upright[upright.Width - 10, 10])).IsTrue();
        _ = await Assert.That(IsRed(upright[10, 10])).IsFalse();
        _ = await Assert.That(Image.Identify(photo.Data).Metadata.ExifProfile).IsNull();
    }

    [Test]
    [Arguments(3000, 2250, 2000, 2000, 1500)]
    [Arguments(1500, 3000, 2000, 1000, 2000)]
    [Arguments(3001, 2000, 2000, 2000, 1333)]
    [Arguments(3000, 2250, 1568, 1568, 1176)]
    public async Task TheLongEdge_IsScaledDownToTheEdge_KeepingTheAspectRatio(
        int width,
        int height,
        int edge,
        int expectedWidth,
        int expectedHeight)
    {
        var photo = PhotoEncoder.Encode(Plain(width, height), edge).Photo;

        _ = await Assert.That((photo.Width, photo.Height)).IsEqualTo((expectedWidth, expectedHeight));
        _ = await Assert.That(Size(photo.Data)).IsEqualTo((expectedWidth, expectedHeight));
    }

    [Test]
    [Arguments(1200, 900, 2000)]
    [Arguments(2000, 1000, 2000)]
    [Arguments(640, 1136, 1568)]
    public async Task APhotoWithinTheEdge_IsNeverEnlarged(int width, int height, int edge)
    {
        var photo = PhotoEncoder.Encode(Plain(width, height), edge).Photo;

        _ = await Assert.That(Size(photo.Data)).IsEqualTo((width, height));
    }

    [Test]
    public async Task APngScreenshot_BecomesAJpeg()
    {
        var photo = PhotoEncoder.Encode(Saved(new Image<Rgb24>(400, 300, new Rgb24(20, 120, 60)), new PngEncoder()), 2000).Photo;

        _ = await Assert.That(Image.DetectFormat(photo.Data)).IsEqualTo(JpegFormat.Instance);
    }

    [Test]
    public async Task APhotoThatFitsAtQuality85_IsSavedAtQuality85()
    {
        var photo = PhotoEncoder.Encode(Noise(1200, 900), edge: 2000).Photo;

        _ = await Assert.That(photo.Quality).IsEqualTo(85);
        _ = await Assert.That(JpegQuality(photo.Data)).IsEqualTo(85);
        _ = await Assert.That(photo.Data.LongLength).IsLessThanOrEqualTo(PhotoEncoder.MaxPhotoBytes);
    }

    [Test]
    public async Task APhotoOver2Point2MegabytesAtQuality85_IsSavedAtQuality70()
    {
        var photo = PhotoEncoder.Encode(Noise(2000, 1700), edge: 2000).Photo;

        _ = await Assert.That(photo.Quality).IsEqualTo(70);
        _ = await Assert.That(JpegQuality(photo.Data)).IsEqualTo(70);
        _ = await Assert.That(photo.Data.LongLength).IsLessThanOrEqualTo(PhotoEncoder.MaxPhotoBytes);
    }

    [Test]
    public async Task APhotoOver2Point2MegabytesAtQuality70_IsSavedAtQuality55()
    {
        var photo = PhotoEncoder.Encode(Noise(2108, 2108), edge: 2576).Photo;

        _ = await Assert.That(photo.Quality).IsEqualTo(55);
        _ = await Assert.That(JpegQuality(photo.Data)).IsEqualTo(55);
        _ = await Assert.That(photo.Data.LongLength).IsLessThanOrEqualTo(PhotoEncoder.MaxPhotoBytes);
    }

    [Test]
    public async Task APhotoStillOver2Point2MegabytesAtQuality55_IsRefused()
    {
        var encoding = PhotoEncoder.Encode(Noise(2400, 2400), edge: 2576);

        _ = await Assert.That(encoding.Photo).IsNull();
        _ = await Assert.That(encoding.Refusal).IsNotNull();
    }

    [Test]
    public async Task AFileThatIsNotAnImage_IsRefused()
    {
        var encoding = PhotoEncoder.Encode("ftypheic, but not a photo"u8.ToArray(), edge: 2000);

        _ = await Assert.That(encoding.Photo).IsNull();
        _ = await Assert.That(encoding.Refusal).IsNotNull();
    }

    private static byte[] Plain(int width, int height) =>
        Saved(new Image<Rgb24>(width, height, new Rgb24(240, 230, 200)), new JpegEncoder { Quality = 90 });

    private static byte[] Noise(int width, int height)
    {
        var image = new Image<Rgb24>(width, height);
        var random = new Random(7);
        image.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < rows.Height; y++)
            {
                random.NextBytes(MemoryMarshal.AsBytes(rows.GetRowSpan(y)));
            }
        });

        return Saved(image, new BmpEncoder());
    }

    private static void Paint(Image<Rgb24> image, Rgb24 colour, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = colour;
            }
        }
    }

    private static byte[] Saved(Image<Rgb24> image, SixLabors.ImageSharp.Formats.IImageEncoder encoder)
    {
        using (image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, encoder);
            return stream.ToArray();
        }
    }

    private static (int Width, int Height) Size(byte[] data)
    {
        var info = Image.Identify(data);
        return (info.Width, info.Height);
    }

    private static int JpegQuality(byte[] data) => Image.Identify(data).Metadata.GetJpegMetadata().Quality;

    private static bool IsRed(Rgb24 pixel) => pixel.R > 200 && pixel.G < 60 && pixel.B < 60;
}
