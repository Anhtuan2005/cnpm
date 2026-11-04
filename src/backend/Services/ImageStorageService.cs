using SkiaSharp;

namespace EcommerceApp.Services;

public interface IImageStorageService
{
    Task<string?> SaveAsWebpAsync(
        IFormFile? file,
        string subfolder,
        int maxWidth,
        int maxHeight,
        int quality = 82,
        long maxBytes = 8 * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task DeleteUploadedFileAsync(string? imageUrl, string subfolder, CancellationToken cancellationToken = default);
}

public class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public ImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task DeleteUploadedFileAsync(string? imageUrl, string subfolder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return Task.CompletedTask;
        }

        var safeFolder = string.Concat(subfolder
            .Trim()
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
        if (string.IsNullOrWhiteSpace(safeFolder))
        {
            return Task.CompletedTask;
        }

        var urlPath = imageUrl.Split('?', '#')[0];
        var expectedPrefix = $"/uploads/{safeFolder}/";
        if (!urlPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", safeFolder));
        var uploadRootPrefix = uploadRoot.EndsWith(Path.DirectorySeparatorChar)
            ? uploadRoot
            : uploadRoot + Path.DirectorySeparatorChar;
        var filePath = Path.GetFullPath(Path.Combine(webRoot, urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!filePath.StartsWith(uploadRootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<string?> SaveAsWebpAsync(
        IFormFile? file,
        string subfolder,
        int maxWidth,
        int maxHeight,
        int quality = 82,
        long maxBytes = 8 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.");
        }

        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException($"Ảnh cần nhỏ hơn {Math.Ceiling(maxBytes / 1024d / 1024d):0}MB.");
        }

        if (maxWidth <= 0 || maxHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidth), "Kích thước ảnh tối đa phải lớn hơn 0.");
        }

        await using var input = file.OpenReadStream();
        using var encoded = new MemoryStream();
        await input.CopyToAsync(encoded, cancellationToken);
        using var data = SKData.CreateCopy(encoded.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new InvalidOperationException("Tệp tải lên không phải ảnh JPG, PNG hoặc WEBP hợp lệ.");
        // Bound decoded memory before allocation, independently of compressed size.
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 40_000_000)
            throw new InvalidOperationException("Ảnh vượt quá giới hạn 40 triệu điểm ảnh.");
        using var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new InvalidOperationException("Tệp tải lên không phải ảnh hợp lệ hoặc đã bị hỏng.");
        var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = rotated ? bitmap.Height : bitmap.Width;
        var height = rotated ? bitmap.Width : bitmap.Height;
        var scale = Math.Min(1d, Math.Min((double)maxWidth / width, (double)maxHeight / height));
        using var output = new SKBitmap(Math.Max(1, (int)Math.Floor(width * scale)), Math.Max(1, (int)Math.Floor(height * scale)),
            SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)output.Width / width, (float)output.Height / height);
            var transform = OrientationTransform(codec.EncodedOrigin, bitmap.Width, bitmap.Height);
            canvas.Concat(in transform);
            using var image = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }

        // A fresh bitmap encodes only pixels; uploaded EXIF/XMP is not propagated.
        using (var image = SKImage.FromBitmap(output))
        using (var webp = image.Encode(SKEncodedImageFormat.Webp, Math.Clamp(quality, 1, 100)))
        {
            if (webp is null) throw new InvalidOperationException("Chưa thể chuyển ảnh sang WEBP.");

            var safeFolder = string.Concat(subfolder
                .Trim()
                .ToLowerInvariant()
                .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
            if (string.IsNullOrWhiteSpace(safeFolder))
            {
                throw new ArgumentException("Thư mục ảnh không hợp lệ.", nameof(subfolder));
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadRoot = Path.Combine(webRoot, "uploads", safeFolder);
            Directory.CreateDirectory(uploadRoot);
            var fileName = $"{Guid.NewGuid():N}.webp";
            var filePath = Path.Combine(uploadRoot, fileName);

            try
            {
                await using var destination = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
                using var content = webp.AsStream();
                await content.CopyToAsync(destination, cancellationToken);
            }
            catch
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                throw;
            }

            return $"/uploads/{safeFolder}/{fileName}";
        }
    }

    private static SKMatrix OrientationTransform(SKEncodedOrigin origin, int w, int h)
    {
        var (a, b, x, c, d, y) = origin switch
        {
            SKEncodedOrigin.TopRight => (-1, 0, w, 0, 1, 0),
            SKEncodedOrigin.BottomRight => (-1, 0, w, 0, -1, h),
            SKEncodedOrigin.BottomLeft => (1, 0, 0, 0, -1, h),
            SKEncodedOrigin.LeftTop => (0, 1, 0, 1, 0, 0),
            SKEncodedOrigin.RightTop => (0, -1, h, 1, 0, 0),
            SKEncodedOrigin.RightBottom => (0, -1, h, -1, 0, w),
            SKEncodedOrigin.LeftBottom => (0, 1, 0, -1, 0, w),
            _ => (1, 0, 0, 0, 1, 0)
        };
        return new SKMatrix { ScaleX = a, SkewX = b, TransX = x, SkewY = c, ScaleY = d, TransY = y, Persp2 = 1 };
    }

}
