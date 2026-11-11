using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace EcommerceApp.Controllers;

public class AccountController : Controller
{
    private const string AttemptCookie = ".Techvora.LoginAttempt";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly IAbandonedCartRecoveryService _abandonedCartRecoveryService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ISprintFeatureService _sprintFeatures;
    private readonly AccountLoginLockoutRecorder _loginLockouts;
    private readonly ILogger<AccountController> _logger;
    private readonly IDataProtector _attemptProtector;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cartService,
        IAbandonedCartRecoveryService abandonedCartRecoveryService,
        IPasswordResetService passwordResetService,
        ISprintFeatureService sprintFeatures,
        AccountLoginLockoutRecorder loginLockouts,
        ILogger<AccountController> logger,
        IDataProtectionProvider dataProtection)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cartService = cartService;
        _abandonedCartRecoveryService = abandonedCartRecoveryService;
        _passwordResetService = passwordResetService;
        _sprintFeatures = sprintFeatures;
        _loginLockouts = loginLockouts;
        _logger = logger;
        _attemptProtector = dataProtection.CreateProtector("Techvora.LoginAttemptFeedback.v1");
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.Email);
        if (user is not null && !await _userManager.GetLockoutEnabledAsync(user))
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
        }

        var adminLocked = user?.IsAdminLocked(DateTimeOffset.UtcNow) == true;
        var previouslyLocked = user is not null && await _userManager.IsLockedOutAsync(user);
        var result = user is null ? IdentitySignInResult.Failed
            : adminLocked ? IdentitySignInResult.LockedOut
            : await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (Request.Cookies.ContainsKey(AttemptCookie))
                Response.Cookies.Delete(AttemptCookie, AttemptCookieOptions());
            if (user is not null)
            {
                await _cartService.MergeGuestCartAsync(user.Id, HttpContext.Session.Id);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin && _sprintFeatures.IsEnabled(3))
                {
                    var reminder = await _abandonedCartRecoveryService.GetLoginReminderAsync(user.Id);
                    if (reminder is not null)
                    {
                        TempData["Success"] = $"Giỏ hàng của bạn đang chờ! Dùng mã {reminder.RecoveryCode} để giảm 30.000 ₫ cho đơn từ 300.000 ₫ trước {reminder.ExpiresAt.ToLocalTime():dd/MM HH:mm}.";
                    }
                }

                if (isAdmin && (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl)))
                {
                    return Redirect(_sprintFeatures.IsEnabled(2) ? "/Admin/Dashboard" : "/Admin/Product");
                }
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        var lockout = _userManager.Options.Lockout;
        var feedback = RecordAttemptFeedback(model.Email, lockout.MaxFailedAccessAttempts, lockout.DefaultLockoutTimeSpan);
        if (result.IsLockedOut && user is not null)
        {
            if (adminLocked)
            {
                var message = user.AdminLockUntil is { } end
                    ? $"Tài khoản đã bị quản trị viên khoá đến {end.ToLocalTime():HH:mm dd/MM/yyyy}."
                    : "Tài khoản đã bị quản trị viên khoá không thời hạn.";
                return LoginRejected(model, $"{message} Vui lòng liên hệ Techvora để được hỗ trợ.", "Tài khoản bị khoá");
            }

            if (!previouslyLocked && user.LockoutEnd is { } lockedUntil && _sprintFeatures.IsEnabled(3))
            {
                try
                {
                    await _loginLockouts.RecordAsync(user.Id, lockedUntil, _userManager.Options.Lockout.MaxFailedAccessAttempts);
                }
                catch (DbUpdateException exception)
                {
                    _logger.LogError(exception, "Could not record automatic lockout for account {UserId}", user.Id);
                }
            }

            if (user.LockoutEnd is { } retryAt)
                return WaitForLogin(model, retryAt);
        }

        if (user is not null && !adminLocked && result == IdentitySignInResult.Failed)
        {
            var remaining = Math.Max(1, lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user));
            return LoginRejected(model, IncorrectLogin(remaining));
        }

        return feedback.Failures >= lockout.MaxFailedAccessAttempts
            ? WaitForLogin(model, feedback.ExpiresAt)
            : LoginRejected(model, IncorrectLogin(lockout.MaxFailedAccessAttempts - feedback.Failures));
    }

    private IActionResult LoginRejected(
        LoginViewModel model, string message, string title = "Đăng nhập chưa thành công", bool isWarning = false)
    {
        model.Notice = new LoginNotice(title, message, isWarning);
        return View(model);
    }

    private static string IncorrectLogin(int remaining) =>
        $"Email hoặc mật khẩu không đúng. Bạn còn {remaining} lần thử đăng nhập.";

    private IActionResult WaitForLogin(LoginViewModel model, DateTimeOffset retryAt)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling((retryAt - DateTimeOffset.UtcNow).TotalMinutes));
        return LoginRejected(model, $"Bạn đã hết lượt thử. Vui lòng thử lại sau {minutes} phút.",
            "Tạm khoá đăng nhập", isWarning: true);
    }

    private LoginAttemptFeedback RecordAttemptFeedback(string email, int limit, TimeSpan duration)
    {
        // Feedback for unverified addresses is bounded in a protected browser cookie.
        // Real account lockouts always use Identity's persisted counter.
        var now = DateTimeOffset.UtcNow;
        var normalized = _userManager.NormalizeName(email) ?? email;
        var identifier = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        LoginAttemptFeedback? previous = null;
        if (Request.Cookies[AttemptCookie] is { Length: <= 1024 } protectedValue)
        {
            try
            {
                previous = JsonSerializer.Deserialize<LoginAttemptFeedback>(_attemptProtector.Unprotect(protectedValue));
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException or JsonException)
            {
                // Ignore expired or malformed browser feedback.
            }
        }
        if (previous is null || previous.Identifier != identifier || previous.ExpiresAt <= now)
            previous = new LoginAttemptFeedback(identifier, 0, now.Add(duration));
        if (previous.Failures >= limit) return previous;

        var failures = previous.Failures + 1;
        var current = previous with { Failures = failures, ExpiresAt = failures == limit ? now.Add(duration) : previous.ExpiresAt };
        var options = AttemptCookieOptions();
        options.Expires = current.ExpiresAt;
        Response.Cookies.Append(AttemptCookie, _attemptProtector.Protect(JsonSerializer.Serialize(current)), options);
        return current;
    }

    private CookieOptions AttemptCookieOptions() => new()
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = Request.IsHttps,
        Path = "/Account/Login"
    };

    private sealed record LoginAttemptFeedback(string Identifier, int Failures, DateTimeOffset ExpiresAt);

    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _cartService.MergeGuestCartAsync(user.Id, HttpContext.Session.Id);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _passwordResetService.SendResetLinkAsync(model.Email);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Link đặt lại mật khẩu không hợp lệ.";
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await _passwordResetService.ResetPasswordAsync(model.Token, model.NewPassword);
        if (success)
        {
            TempData["Success"] = "Đặt lại mật khẩu thành công. Hãy đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, error ?? "Có lỗi xảy ra.");
        return View(model);
    }

    [Authorize]
    [HttpGet("/account/profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address
        });
    }

    [Authorize]
    [HttpPost("/account/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email ?? string.Empty;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost("/account/delete-address")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.Address = null;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Không thể xóa địa chỉ lúc này. Vui lòng thử lại.";
            return RedirectToAction(nameof(Profile));
        }

        TempData["Success"] = "Đã xóa địa chỉ đã lưu.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet("/account/change-password")]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost("/account/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Đã đổi mật khẩu.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
