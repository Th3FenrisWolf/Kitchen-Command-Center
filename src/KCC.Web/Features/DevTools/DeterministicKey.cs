using System.Security.Cryptography;
using System.Text;

namespace KCC.Web.Features.DevTools;

public static class DeterministicKey
{
    public static Guid For(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed)).AsSpan(0, 16);

        // The backoffice rejects keys that are not RFC 9562 UUIDs. Guid reads its first three fields little-endian,
        // so the version nibble (8, custom) lives in byte 7 rather than byte 6.
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
