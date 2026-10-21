using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers.Admin;

[MinimumSprint(2)]
[Authorize(Roles = "Admin")]
[Route("Admin/Review")]
public class AdminReviewController : Controller
{
    private readonly AppDbContext _db;
    private readonly IUserNotificationService? _notificationService;

    public AdminReviewController(AppDbContext db, IUserNotificationService? notificationService = null)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var reviews = await _db.Reviews
            .Include(review => review.Product)
            .Include(review => review.User)
            .Include(review => review.Images.OrderBy(image => image.SortOrder))
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync();
        return View("~/Views/Admin/Review/Index.cshtml", reviews);
    }

    [HttpPost("Reply/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? reply)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is null)
        {
            return NotFound();
        }

        var content = reply?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
        {
            TempData["Error"] = "Phản hồi cần có nội dung và không vượt quá 500 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        var firstReply = string.IsNullOrWhiteSpace(review.StoreReply);
        review.StoreReply = content;
        review.StoreRepliedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (firstReply && _notificationService is not null)
        {
            await _notificationService.CreateAsync(
                review.UserId,
                "Cửa hàng đã phản hồi đánh giá",
                "Techvora đã phản hồi đánh giá sản phẩm của bạn.",
                NotificationTypes.Review,
                $"/Product/Detail/{review.ProductId}");
        }

        TempData["Success"] = "Đã đăng phản hồi công khai cho đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}
