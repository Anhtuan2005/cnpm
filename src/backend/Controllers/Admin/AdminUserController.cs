using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers.Admin;

[MinimumSprint(3)]
[Authorize(Roles = "Admin")]
[Route("Admin/User")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminUserController> _logger;
    private readonly AdminUserAccessService _access;
    private readonly AppDbContext _db;

    public AdminUserController(UserManager<ApplicationUser> userManager, ILogger<AdminUserController> logger,
        AdminUserAccessService access, AppDbContext db)
    {
        _userManager = userManager;
        _logger = logger;
        _access = access;
        _db = db;
    }

    [HttpGet("Review/{id}")]
    public Task<IActionResult> Review(string id) => ShowAccountAsync(id, false);

    [HttpGet("ConfirmRole/{id}")]
    public Task<IActionResult> ConfirmRole(string id) => ShowAccountAsync(id, true);

    private async Task<IActionResult> ShowAccountAsync(string id, bool confirmRole)
    {
        var account = await _userManager.FindByIdAsync(id);
        if (account is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(account);
        var history = await _db.AccountAccessEvents.AsNoTracking()
            .Where(entry => entry.UserId == id)
            .OrderByDescending(entry => entry.Id)
            .Take(30)
            .ToListAsync();
        return View("~/Views/Admin/User/Review.cshtml", new AdminUserAccessViewModel(
            account, roles, history, id == ActorId, confirmRole));
    }

    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.OrderBy(user => user.FullName).ToListAsync(cancellationToken);
        var roles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
        {
            roles[user.Id] = await _userManager.GetRolesAsync(user);
        }

        return View("~/Views/Admin/User/Index.cshtml", new AdminUserIndexViewModel(users, roles));
    }

    [HttpGet("Statuses")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken)
    {
        var accounts = await _db.Users.AsNoTracking()
            .Select(user => new { user.Id, user.AdminLockedAt, user.AdminLockUntil, user.LockoutEnabled, user.LockoutEnd })
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        return Json(accounts.Select(user => new
        {
            user.Id,
            status = AdminUserLockStatus.From(user.AdminLockedAt, user.AdminLockUntil, user.LockoutEnabled, user.LockoutEnd, now)
        }));
    }

    [HttpPost("ToggleLock/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id, bool? locked, string? reason, AdminLockDuration? duration)
    {
        if (!locked.HasValue) return BadRequest();
        Record(await _access.SetLockAsync(ActorId, id, locked.Value, reason, duration), id);
        return RedirectToAction(nameof(Review), new { id });
    }

    [HttpPost("MakeAdmin/{id}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("admin-role-confirmation")]
    public Task<IActionResult> MakeAdmin(string id, string? reason, string? currentPassword) =>
        ChangeRoleAsync(id, true, reason, currentPassword);

    [HttpPost("RemoveAdmin/{id}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("admin-role-confirmation")]
    public Task<IActionResult> RemoveAdmin(string id, string? reason, string? currentPassword) =>
        ChangeRoleAsync(id, false, reason, currentPassword);

    private async Task<IActionResult> ChangeRoleAsync(string id, bool admin, string? reason, string? currentPassword)
    {
        var result = await _access.SetAdminAsync(ActorId, id, admin, reason, currentPassword);
        Record(result, id);
        return RedirectToAction(result.Succeeded ? nameof(Review) : nameof(ConfirmRole), new { id });
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private void Record(UserAccessResult result, string userId)
    {
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        if (result.Succeeded) _logger.LogInformation("Admin {Admin} updated access for user {UserId}: {Result}", User.Identity?.Name, userId, result.Message);
    }
}
