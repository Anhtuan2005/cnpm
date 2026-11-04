using EcommerceApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using SkiaSharp;

namespace EcommerceApp.Tests;

public class UploadValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Valid_image_is_resized_oriented_encoded_and_deletable(bool rotate)
    {
        var root = Path.Combine(Path.GetTempPath(), "TechvoraUploadTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var bitmap = new SKBitmap(80, 40);
            using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.Red);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(rotate ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 95);
            var bytes = data.ToArray();
            if (rotate)
            {
                // JPEG APP1 with EXIF orientation 6: 90 degrees clockwise.
                var exif = Convert.FromHexString("FFE1002245786966000049492A0008000000010012010300010000000600000000000000");
                bytes = bytes[..2].Concat(exif).Concat(bytes[2..]).ToArray();
            }
            using var input = new MemoryStream(bytes);
            var file = new FormFile(input, 0, input.Length, "image", rotate ? "phone.jpg" : "phone.png")
            { Headers = new HeaderDictionary(), ContentType = rotate ? "image/jpeg" : "image/png" };
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.WebRootPath).Returns(root);
            var service = new ImageStorageService(environment.Object);
            var url = await service.SaveAsWebpAsync(file, "products", 20, 20);
            Assert.StartsWith("/uploads/products/", url);
            var path = Path.Combine(root, url!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            using var encoded = SKData.CreateCopy(File.ReadAllBytes(path));
            using var codec = SKCodec.Create(encoded);
            Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
            Assert.Equal(rotate ? 10 : 20, codec.Info.Width);
            Assert.Equal(rotate ? 20 : 10, codec.Info.Height);
            Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
            await service.DeleteUploadedFileAsync(url, "reviews");
            Assert.True(File.Exists(path));
            await service.DeleteUploadedFileAsync(url, "products");
            Assert.False(File.Exists(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Corrupted_png_returns_a_validation_error_instead_of_an_image_decoder_exception()
    {
        // Valid PNG signature with a truncated IHDR chunk.
        using var data = new MemoryStream(Convert.FromHexString("89504E470D0A1A0A0000000D4948445200000001"));
        var file = new FormFile(data, 0, data.Length, "imageFile", "broken.png")
        { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var service = new ImageStorageService(Mock.Of<IWebHostEnvironment>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsWebpAsync(file, "banners", 100, 100));
    }
}
