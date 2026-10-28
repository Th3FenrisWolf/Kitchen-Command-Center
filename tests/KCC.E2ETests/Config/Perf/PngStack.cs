using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace KCC.E2ETests.Config.Perf;

public static class PngStack
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    private static readonly uint[] CrcTable = [.. Enumerable.Range(0, 256).Select(index =>
    {
        var value = (uint)index;
        for (var bit = 0; bit < 8; bit++)
        {
            value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
        }

        return value;
    })];

    public static IReadOnlyList<(int Y, int Height)> Segments(int cssHeight, float scale, int maxDeviceRows)
    {
        var step = Math.Max(1, (int)Math.Floor(maxDeviceRows / scale));
        return [.. Enumerable.Range(0, (cssHeight + step - 1) / step).Select(index => (index * step, Math.Min(step, cssHeight - (index * step))))];
    }

    public static byte[] Vertical(IReadOnlyList<byte[]> strips)
    {
        var decoded = strips.Select(DecodeWithChannels).ToList();
        var (width, _, _, channels) = decoded[0];
        if (decoded.Any(strip => strip.Width != width || strip.Channels != channels))
        {
            throw new InvalidOperationException("Only strips of one width and one colour type stack.");
        }

        var height = decoded.Sum(strip => strip.Height);
        var stride = width * channels;
        var filtered = new byte[height * (stride + 1)];
        var row = 0;
        foreach (var strip in decoded)
        {
            for (var y = 0; y < strip.Height; y++, row++)
            {
                strip.Pixels.AsSpan(y * stride, stride).CopyTo(filtered.AsSpan((row * (stride + 1)) + 1));
            }
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = channels == 4 ? (byte)6 : (byte)2;
        using var png = new MemoryStream();
        png.Write(Signature);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", Compress(filtered));
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    public static (int Width, int Height, byte[] Pixels) Decode(byte[] png)
    {
        var (width, height, pixels, _) = DecodeWithChannels(png);
        return (width, height, pixels);
    }

    private static (int Width, int Height, byte[] Pixels, int Channels) DecodeWithChannels(byte[] png)
    {
        if (!png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
        {
            throw new InvalidOperationException("Not a PNG.");
        }

        int width = 0, height = 0, channels = 0;
        using var data = new MemoryStream();
        for (var offset = Signature.Length; offset < png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var body = png.AsSpan(offset + 8, length);
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(body);
                height = BinaryPrimitives.ReadInt32BigEndian(body[4..]);
                channels = body[9] switch
                {
                    6 => 4,
                    2 => 3,
                    _ => 0,
                };
                if (body[8] != 8 || channels == 0 || body[12] != 0)
                {
                    throw new InvalidOperationException("Only 8-bit, non-interlaced RGB or RGBA PNGs stack.");
                }
            }
            else if (type == "IDAT")
            {
                data.Write(body);
            }

            offset += 12 + length;
        }

        data.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionMode.Decompress))
        {
            zlib.CopyTo(inflated);
        }

        return (width, height, Unfilter(inflated.ToArray(), width * channels, height, channels), channels);
    }

    private static byte[] Unfilter(byte[] filtered, int stride, int height, int channels)
    {
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var filter = filtered[y * (stride + 1)];
            for (var x = 0; x < stride; x++)
            {
                var at = (y * stride) + x;
                int left = x >= channels ? pixels[at - channels] : 0;
                int up = y > 0 ? pixels[at - stride] : 0;
                int corner = x >= channels && y > 0 ? pixels[at - stride - channels] : 0;
                var predictor = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, corner),
                    _ => throw new InvalidOperationException($"Unknown PNG row filter {filter}."),
                };
                pixels[at] = (byte)(filtered[(y * (stride + 1)) + 1 + x] + predictor);
            }
        }

        return pixels;
    }

    private static int Paeth(int left, int up, int corner)
    {
        var estimate = left + up - corner;
        var toLeft = Math.Abs(estimate - left);
        var toUp = Math.Abs(estimate - up);
        var toCorner = Math.Abs(estimate - corner);
        return toLeft <= toUp && toLeft <= toCorner ? left : toUp <= toCorner ? up : corner;
    }

    private static byte[] Compress(byte[] filtered)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(filtered);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] body)
    {
        var typed = new byte[4 + body.Length];
        Encoding.ASCII.GetBytes(type, typed);
        body.CopyTo(typed, 4);
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, body.Length);
        stream.Write(word);
        stream.Write(typed);
        BinaryPrimitives.WriteUInt32BigEndian(word, Crc(typed));
        stream.Write(word);
    }

    private static uint Crc(byte[] bytes)
    {
        var crc = 0xFFFFFFFF;
        foreach (var value in bytes)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }
}
