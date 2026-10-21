using EcommerceApp.Data;
using EcommerceApp.Hubs;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EcommerceApp.Controllers;

[MinimumSprint(2)]
[Authorize]
public class ReviewController : Controller
{
    private const int MaxReviewImages = 4;
    private const long MaxReviewImageBytes = 3 * 1024 * 1024;
    private static readonly HashSet<string> AllowedReviewImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly AppDbContext _db;
    private readonly IImageStorageService _imageStorage;
    private readonly IUserNotificationService? _notificationService;
    private readonly IHubContext<AdminNotificationHub>? _adminNotificationHub;

    public ReviewController(
        AppDbContext db,
        IImageStorageService imageStorage,
        IUserNotificationService? notificationService = null,
        IHubContext<AdminNotificationHub>? adminNotificationHub = null)
    {
        _db = db;
        _imageStorage = imageStorage;
        _notificationService = notificationService;
        _adminNotificationHub = adminNotificationHub;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("review")]
    public async Task<IActionResult> Submit(int productId, [Range(1, 5)] int rating, [Required, MinLength(10), MaxLength(1000)] string comment, List<IFormFile>? images)
    {
        if (User.IsInRole("Admin"))
        {
            return Json(new { success = false, message = "Tài khoản admin không thể đánh giá sản phẩm." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var canReview = await _db.Orders.AnyAsync(order => order.UserId == userId && order.Status == OrderStatuses.Delivered && order.Items.Any(item => item.ProductId == productId));
        if (!canReview)
        {
            return Json(new { success = false, message = "Bạn cần mua sản phẩm trước khi đánh giá." });
        }

        if (await _db.Reviews.AnyAsync(review => review.UserId == userId && review.ProductId == productId))
        {
            return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này." });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Vui lòng chọn sao và nhập nhận xét từ 10 ký tự." });
        }

        var imageFiles = (images ?? new List<IFormFile>()).Where(file => file.Length > 0).Take(MaxReviewImages + 1).ToList();
        if (imageFiles.Count > MaxReviewImages)
        {
            return Json(new { success = false, message = $"Bạn chỉ có thể tải tối đa {MaxReviewImages} ảnh cho mỗi đánh giá." });
        }

        var imageError = ValidateReviewImages(imageFiles);
        if (!string.IsNullOrWhiteSpace(imageError))
        {
            return Json(new { success = false, message = imageError });
        }

        List<string> savedImages;
        try
        {
            savedImages = await SaveReviewImagesAsync(imageFiles);
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment.Trim()
        };

        foreach (var image in savedImages.Select((url, index) => new ReviewImage { ImageUrl = url, SortOrder = index }))
        {
            review.Images.Add(image);
        }

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        if (_notificationService is not null)
        {
            await _notificationService.CreateForAdminsAsync(
                "Có đánh giá mới",
                $"Khách hàng vừa đánh giá sản phẩm #{productId}.",
                NotificationTypes.Review,
                "/Admin/Review");
        }
        if (_adminNotificationHub is not null)
        {
            await _adminNotificationHub.Clients
                .Group(AdminNotificationHub.AdminGroup)
                .SendAsync("ReviewCreated", new AdminReviewCreatedMessage(
                    review.Id,
                    productId,
                    "/Admin/Review"));
        }

        return Json(new
        {
            success = true,
            message = "Cảm ơn bạn đã đánh giá.",
            stats = await GetReviewStatsAsync(productId),
            review = new
            {
                id = review.Id,
                user = user?.FullName ?? User.Identity?.Name ?? "Khách hàng",
                date = review.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"),
                rating = review.Rating,
                comment = review.Comment,
                images = review.Images.OrderBy(image => image.SortOrder).Select(image => image.ImageUrl)
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int reviewId, [Range(1, 5)] int rating, [Required, MinLength(10), MaxLength(1000)] string comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || User.IsInRole("Admin"))
        {
            return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa đánh giá này." });
        }

        var review = await _db.Reviews
            .SingleOrDefaultAsync(item => item.Id == reviewId && item.UserId == userId);
        if (review is null)
        {
            return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa." });
        }

        var normalizedComment = comment?.Trim() ?? string.Empty;
        if (!ModelState.IsValid || rating is < 1 or > 5 || normalizedComment.Length is < 10 or > 1000)
        {
            return Json(new { success = false, message = "Vui lòng chọn số sao và nhập nhận xét từ 10 đến 1000 ký tự." });
        }

        review.Rating = rating;
        review.Comment = normalizedComment;
        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Đã cập nhật đánh giá.",
            review = new { id = review.Id, rating = review.Rating, comment = review.Comment },
            stats = await GetReviewStatsAsync(review.ProductId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int reviewId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || User.IsInRole("Admin"))
        {
            return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá này." });
        }

        var review = await _db.Reviews
            .Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == reviewId && item.UserId == userId);
        if (review is null)
        {
            return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa." });
        }

        var productId = review.ProductId;
        var imageUrls = review.Images.Select(image => image.ImageUrl).ToList();
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        foreach (var imageUrl in imageUrls)
        {
            try
            {
                await _imageStorage.DeleteUploadedFileAsync(imageUrl, "reviews");
            }
            catch (IOException)
            {
                // The review is already deleted; a failed file cleanup must not turn that into a failed request.
            }
            catch (UnauthorizedAccessException)
            {
                // The review is already deleted; a failed file cleanup must not turn that into a failed request.
            }
        }

        return Json(new
        {
            success = true,
            message = "Đã xóa đánh giá.",
            stats = await GetReviewStatsAsync(productId)
        });
    }

    private async Task<ReviewStatistics> GetReviewStatsAsync(int productId)
    {
        var reviews = await _db.Reviews
            .Where(review => review.ProductId == productId)
            .Select(review => new { review.Rating, ImageCount = review.Images.Count })
            .ToListAsync();
        var distribution = Enumerable.Range(1, 5)
            .ToDictionary(star => star, star => reviews.Count(review => review.Rating == star));

        return new ReviewStatistics(
            reviews.Count,
            reviews.Count == 0 ? 0 : reviews.Average(review => review.Rating),
            reviews.Sum(review => review.ImageCount),
            distribution);
    }

    private sealed record ReviewStatistics(int Count, double Average, int PhotoCount, Dictionary<int, int> Distribution);

    private static string? ValidateReviewImages(IEnumerable<IFormFile> images)
    {
        foreach (var image in images)
        {
            var extension = Path.GetExtension(image.FileName);
            if (!AllowedReviewImageExtensions.Contains(extension) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Ảnh đánh giá chỉ hỗ trợ JPG, PNG hoặc WEBP.";
            }

            if (image.Length > MaxReviewImageBytes)
            {
                return "Mỗi ảnh đánh giá cần nhỏ hơn 3MB.";
            }
        }

        return null;
    }

    private async Task<List<string>> SaveReviewImagesAsync(IEnumerable<IFormFile> images)
    {
        var urls = new List<string>();
        foreach (var image in images)
        {
            var url = await _imageStorage.SaveAsWebpAsync(
                image,
                "reviews",
                1600,
                1600,
                80,
                MaxReviewImageBytes);
            if (url is not null)
            {
                urls.Add(url);
            }
        }

        return urls;
    }
}
