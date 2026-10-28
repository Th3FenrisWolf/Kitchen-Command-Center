using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using KCC.E2ETests.Config.Perf;

namespace KCC.E2ETests.Features.Performance;

public class PngStackTests
{
    [Test]
    public async Task Segments_CoverThePageTopToBottom_InStripsNoTallerThanTheDeviceLimit()
    {
        _ = await Assert.That(PngStack.Segments(cssHeight: 3000, scale: 3, maxDeviceRows: 4096))
            .IsEquivalentTo([(0, 1365), (1365, 1365), (2730, 270)]);
        _ = await Assert.That(PngStack.Segments(cssHeight: 900, scale: 1, maxDeviceRows: 4096)).IsEquivalentTo([(0, 900)]);
        _ = await Assert.That(PngStack.Segments(cssHeight: 8192, scale: 1, maxDeviceRows: 4096)).IsEquivalentTo([(0, 4096), (4096, 4096)]);
    }

    [Test]
    [Arguments(4)]
    [Arguments(3)]
    public async Task Vertical_StacksTheStrips_Unfiltering_EveryRowFilter(int channels)
    {
        var top = Pixels(width: 5, height: 5, channels, seed: 1);
        var bottom = Pixels(width: 5, height: 3, channels, seed: 2);

        var stacked = PngStack.Vertical([Encode(5, 5, channels, top), Encode(5, 3, channels, bottom)]);
        var (width, height, pixels) = PngStack.Decode(stacked);

        _ = await Assert.That(width).IsEqualTo(5);
        _ = await Assert.That(height).IsEqualTo(8);
        _ = await Assert.That(pixels).IsEquivalentTo(top.Concat(bottom).ToArray());
    }

    [Test]
    public async Task Vertical_RefusesStripsOfDifferentWidths()
    {
        _ = await Assert.That(() => PngStack.Vertical([Encode(2, 1, 4, new byte[8]), Encode(3, 1, 4, new byte[12])]))
            .Throws<InvalidOperationException>();
    }

    private static byte[] Pixels(int width, int height, int channels, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height * channels];
        random.NextBytes(pixels);
        return pixels;
    }

    private static byte[] Encode(int width, int height, int channels, byte[] pixels)
    {
        var stride = width * channels;
        var filtered = new byte[height * (stride + 1)];
        for (var y = 0; y < height; y++)
        {
            var filter = (byte)(y % 5);
            filtered[y * (stride + 1)] = filter;
            for (var x = 0; x < stride; x++)
            {
                int Raw(int row, int column) => row < 0 || column < 0 ? 0 : pixels[(row * stride) + column];
                int left = Raw(y, x - channels), up = Raw(y - 1, x), corner = Raw(y - 1, x - channels);
                var predictor = filter switch
                {
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, corner),
                    _ => 0,
                };
                filtered[(y * (stride + 1)) + 1 + x] = (byte)(Raw(y, x) - predictor);
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(filtered);
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = channels == 4 ? (byte)6 : (byte)2;
        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Chunk(png, "IHDR", header);
        var data = compressed.ToArray();
        Chunk(png, "IDAT", data[..(data.Length / 2)]);
        Chunk(png, "IDAT", data[(data.Length / 2)..]);
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(Encoding.ASCII.GetBytes(type));
        stream.Write(data);
        stream.Write(new byte[4]);
    }

    private static int Paeth(int left, int up, int corner)
    {
        var estimate = left + up - corner;
        var toLeft = Math.Abs(estimate - left);
        var toUp = Math.Abs(estimate - up);
        var toCorner = Math.Abs(estimate - corner);
        return toLeft <= toUp && toLeft <= toCorner ? left : toUp <= toCorner ? up : corner;
    }
}
