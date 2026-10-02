using System.Security.Cryptography;
using System.Text;

namespace KCC.Web.Features.DevTools.RecipeSeed;

// The same seed always yields the same key, so a second run finds what the first one created.
public static class SeedKeys
{
    public static Guid Recipe(string recipeName) => Key($"recipe::{recipeName}");

    public static Guid Variant(string recipeName, string variantName) => Key($"variant::{recipeName}::{variantName}");

    public static Guid Author(string userName) => Key($"author::{userName}");

    public static Guid Reviewer(string recipeName, int index) => Key($"review::{recipeName}::{index}");

    private static Guid Key(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed)).AsSpan(0, 16);

        // The backoffice rejects keys that are not RFC 9562 UUIDs. Guid reads its first three fields little-endian,
        // so the version nibble (8, custom) lives in byte 7 rather than byte 6.
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
