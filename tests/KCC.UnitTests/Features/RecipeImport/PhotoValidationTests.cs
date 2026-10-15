using System.Text;
using KCC.Web.Features.RecipeImport;
using Microsoft.AspNetCore.Http;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.RecipeImport;

public class PhotoValidationTests
{
    private const int FourMegabytes = 4_000_000;
    private const int TwentyTwoMegabytes = 22_000_000;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] WebP = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8];
    private static readonly byte[] Gif87a = [.. "GIF87a"u8, 0x01, 0x00, 0x01, 0x00];
    private static readonly byte[] Gif89a = [.. "GIF89a"u8, 0x01, 0x00, 0x01, 0x00];

    [Test]
    public async Task APhotoOfEachSupportedType_IsAccepted_WithTheMediaTypeItsLeadingBytesName()
    {
        var photos = await PhotoValidation.ReadAsync(
            [File(Jpeg), File(Png), File(WebP), File(Gif87a), File(Gif89a)],
            CancellationToken.None);

        _ = await Assert.That(photos.Select(photo => photo.MediaType)).IsEquivalentTo(
            ["image/jpeg", "image/png", "image/webp", "image/gif", "image/gif"],
            CollectionOrdering.Matching);
        _ = await Assert.That(photos.Select(photo => Convert.ToBase64String(photo.Data))).IsEquivalentTo(
            new[] { Jpeg, Png, WebP, Gif87a, Gif89a }.Select(Convert.ToBase64String),
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task TheLeadingBytes_DecideTheType_WhateverTheFileSaysItIs()
    {
        var photos = await PhotoValidation.ReadAsync([File(Png, "photo.jpg", "image/jpeg")], CancellationToken.None);

        _ = await Assert.That(photos.Single().MediaType).IsEqualTo("image/png");
    }

    [Test]
    [Arguments(1)]
    [Arguments(10)]
    public async Task OneToTenPhotos_AreAccepted(int count)
    {
        var photos = await PhotoValidation.ReadAsync([.. Enumerable.Repeat(Jpeg, count).Select(bytes => File(bytes))], CancellationToken.None);

        _ = await Assert.That(photos.Count).IsEqualTo(count);
    }

    [Test]
    [Arguments(0)]
    [Arguments(11)]
    public async Task NoPhoto_OrMoreThanTen_IsRefused(int count)
    {
        var photos = await PhotoValidation.ReadAsync([.. Enumerable.Repeat(Jpeg, count).Select(bytes => File(bytes))], CancellationToken.None);

        _ = await Assert.That(photos).IsNull();
    }

    [Test]
    public async Task APhotoOfFourMegabytes_IsAccepted()
    {
        var photos = await PhotoValidation.ReadAsync([File(Sized(Jpeg, FourMegabytes))], CancellationToken.None);

        _ = await Assert.That(photos.Single().Data.Length).IsEqualTo(FourMegabytes);
    }

    [Test]
    public async Task APhotoOverFourMegabytes_IsRefused()
    {
        var photos = await PhotoValidation.ReadAsync([File(Sized(Jpeg, FourMegabytes + 1))], CancellationToken.None);

        _ = await Assert.That(photos).IsNull();
    }

    [Test]
    public async Task PhotosOfTwentyTwoMegabytesTogether_AreAccepted()
    {
        var photos = await PhotoValidation.ReadAsync(PhotosTogether(TwentyTwoMegabytes), CancellationToken.None);

        _ = await Assert.That(photos.Sum(photo => (long)photo.Data.Length)).IsEqualTo((long)TwentyTwoMegabytes);
    }

    [Test]
    public async Task PhotosOverTwentyTwoMegabytesTogether_AreRefused()
    {
        var photos = await PhotoValidation.ReadAsync(PhotosTogether(TwentyTwoMegabytes + 1), CancellationToken.None);

        _ = await Assert.That(photos).IsNull();
    }

    [Test]
    [Arguments("heic")]
    [Arguments("pdf")]
    [Arguments("text")]
    [Arguments("empty")]
    [Arguments("riff audio")]
    [Arguments("short riff")]
    [Arguments("short jpeg")]
    public async Task AFileThatIsNotAJpegPngWebPOrGif_IsRefused(string kind)
    {
        byte[] bytes = kind switch
        {
            "heic" => [0x00, 0x00, 0x00, 0x18, .. "ftypheic"u8, 0x00, 0x00, 0x00, 0x00],
            "pdf" => [.. "%PDF-1.7\n"u8],
            "text" => Encoding.UTF8.GetBytes("Two cups of flour."),
            "empty" => [],
            "riff audio" => [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVEfmt "u8],
            "short riff" => [.. "RIFF"u8, 0x24, 0x00],
            "short jpeg" => [0xFF, 0xD8],
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var photos = await PhotoValidation.ReadAsync([File(bytes)], CancellationToken.None);

        _ = await Assert.That(photos).IsNull();
    }

    [Test]
    public async Task OneUnsupportedFile_RefusesTheWholeImport()
    {
        var photos = await PhotoValidation.ReadAsync(
            [File(Jpeg), File([.. "%PDF-1.7\n"u8]), File(Png)],
            CancellationToken.None);

        _ = await Assert.That(photos).IsNull();
    }

    private static IReadOnlyList<IFormFile> PhotosTogether(int totalBytes)
    {
        var whole = totalBytes / FourMegabytes;
        var rest = totalBytes % FourMegabytes;
        return
        [
            .. Enumerable.Range(0, whole).Select(_ => File(Sized(Jpeg, FourMegabytes))),
            .. rest > 0 ? [File(Sized(Png, rest))] : Array.Empty<IFormFile>(),
        ];
    }

    private static byte[] Sized(byte[] leadingBytes, int length)
    {
        var bytes = new byte[length];
        leadingBytes.CopyTo(bytes, 0);
        return bytes;
    }

    private static FormFile File(byte[] bytes, string fileName = "photo", string contentType = "application/octet-stream") =>
        new(new MemoryStream(bytes), 0, bytes.Length, "photos", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
}
