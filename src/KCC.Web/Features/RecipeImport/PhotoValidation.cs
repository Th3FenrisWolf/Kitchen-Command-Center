using System.Net.Mime;

namespace KCC.Web.Features.RecipeImport;

public static class PhotoValidation
{
    public const int MaxPhotos = 10;
    public const long MaxPhotoBytes = 4_000_000;
    public const long MaxTotalBytes = 22_000_000;

    private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task<IReadOnlyList<ReadPhoto>> ReadAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files is not { Count: >= 1 and <= MaxPhotos }
            || files.Any(file => file.Length > MaxPhotoBytes)
            || files.Sum(file => file.Length) > MaxTotalBytes)
        {
            return null;
        }

        var photos = new List<ReadPhoto>(files.Count);
        foreach (var file in files)
        {
            var data = new byte[file.Length];
            await using (var stream = file.OpenReadStream())
            {
                await stream.ReadExactlyAsync(data, cancellationToken);
            }

            if (MediaTypeOf(data) is not { } mediaType)
            {
                return null;
            }

            photos.Add(new ReadPhoto(mediaType, data));
        }

        return photos;
    }

    private static string MediaTypeOf(ReadOnlySpan<byte> data) =>
        data.StartsWith(Jpeg) ? MediaTypeNames.Image.Jpeg
        : data.StartsWith(Png) ? MediaTypeNames.Image.Png
        : data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8) ? MediaTypeNames.Image.Gif
        : IsWebP(data) ? MediaTypeNames.Image.Webp
        : null;

    private static bool IsWebP(ReadOnlySpan<byte> data) =>
        data.Length >= 12 && data.StartsWith("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8);
}
