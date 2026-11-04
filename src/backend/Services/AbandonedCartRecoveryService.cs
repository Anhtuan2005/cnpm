using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EcommerceApp.Services;

public class AbandonedCartRecoveryService : IAbandonedCartRecoveryService
{
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(24);
    private static readonly TimeSpan VoucherLifetime = TimeSpan.FromHours(48);
    private static readonly TimeSpan LoginReminderCooldown = TimeSpan.FromHours(12);
    private const decimal RecoveryDiscount = 30_000m;
    private const decimal MinOrderAmount = 300_000m;

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AbandonedCartRecoveryService> _logger;

    public AbandonedCartRecoveryService(
        AppDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AbandonedCartRecoveryService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessDueCartsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.Subtract(AbandonedAfter);
        var carts = await _db.Carts
            .Include(cart => cart.User)
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .Where(cart => cart.UserId != null && cart.UpdatedAt <= cutoff
                && cart.Items.Any(item => item.Product != null && item.Product.Stock > 0)
                && !_db.AbandonedCartReminders.Any(row => row.CartId == cart.Id && row.CartUpdatedAt == cart.UpdatedAt)
                && !_db.Orders.Any(order => order.UserId == cart.UserId && order.CreatedAt >= cart.UpdatedAt)
                && !_db.UserRoles.Any(userRole => userRole.UserId == cart.UserId
                    && _db.Roles.Any(role => role.Id == userRole.RoleId && role.Name == "Admin")))
            .OrderBy(cart => cart.UpdatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var cart in carts)
        {
            await CreateReminderIfNeededAsync(cart, now, sendEmail: true, cancellationToken);
        }
    }

    public async Task<AbandonedCartReminderMessage?> GetLoginReminderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || await IsAdminUserAsync(userId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var cart = await _db.Carts
            .Include(row => row.User)
            .Include(row => row.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(row => row.UserId == userId && row.Items.Any(), cancellationToken);

        if (cart is not null && cart.UpdatedAt <= now.Subtract(AbandonedAfter))
        {
            await CreateReminderIfNeededAsync(cart, now, sendEmail: false, cancellationToken);
        }

        var reminder = await _db.AbandonedCartReminders
            .Include(row => row.Cart)
            .ThenInclude(row => row!.Items)
            .Include(row => row.Voucher)
            .Where(row => row.UserId == userId
                && row.ExpiresAt > now
                && (row.LastShownAt == null || row.LastShownAt <= now.Subtract(LoginReminderCooldown))
                && row.Cart != null
                && row.Cart.UpdatedAt == row.CartUpdatedAt
                && row.Cart.Items.Any(item => item.Product != null && item.Product.Stock > 0)
                && !_db.Orders.Any(order => order.UserId == userId && order.CreatedAt >= row.CartUpdatedAt)
                && row.Voucher != null
                && row.Voucher.IsActive
                && row.Voucher.UsedCount < row.Voucher.UsageLimit
                && row.Voucher.StartDate <= now
                && row.Voucher.EndDate > now)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (reminder is null)
        {
            return null;
        }

        reminder.LastShownAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return new AbandonedCartReminderMessage(
            reminder.RecoveryCode,
            reminder.ExpiresAt,
            reminder.Cart?.Items.Sum(item => item.Quantity) ?? 0);
    }

    private async Task CreateReminderIfNeededAsync(Cart cart, DateTime now, bool sendEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cart.UserId) || cart.User is null || string.IsNullOrWhiteSpace(cart.User.Email)
            || !cart.Items.Any(item => item.Product is { Stock: > 0, IsDeleted: false }))
        {
            return;
        }

        if (await IsAdminUserAsync(cart.UserId, cancellationToken))
        {
            return;
        }

        var hasOrderAfterCartUpdate = await _db.Orders
            .AnyAsync(order => order.UserId == cart.UserId && order.CreatedAt >= cart.UpdatedAt, cancellationToken);
        if (hasOrderAfterCartUpdate)
        {
            return;
        }

        var exists = await _db.AbandonedCartReminders
            .AnyAsync(row => row.CartId == cart.Id && row.CartUpdatedAt == cart.UpdatedAt, cancellationToken);
        if (exists)
        {
            return;
        }

        var voucher = await CreateRecoveryVoucherAsync(cart.UserId, now, cancellationToken);
        var reminder = new AbandonedCartReminder
        {
            CartId = cart.Id,
            UserId = cart.UserId,
            Voucher = voucher,
            RecoveryCode = voucher.Code,
            CartUpdatedAt = cart.UpdatedAt,
            CreatedAt = now,
            ExpiresAt = voucher.EndDate
        };

        var notification = new UserNotification
        {
            UserId = cart.UserId, Title = "Giỏ hàng của bạn đang chờ",
            Message = $"Dùng mã {voucher.Code} để giảm 30.000 ₫ cho đơn từ 300.000 ₫ trong 48 giờ.",
            Type = NotificationTypes.Voucher, LinkUrl = "/Cart"
        };
        _db.AbandonedCartReminders.Add(reminder);
        _db.UserNotifications.Add(notification);
        try
        {
            // The unique cart/version index also protects simultaneous login and background scans.
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _db.Entry(reminder).State = EntityState.Detached;
            _db.Entry(voucher).State = EntityState.Detached;
            _db.Entry(notification).State = EntityState.Detached;
            return;
        }

        if (!sendEmail || !_configuration.GetValue<bool>("Email:Enabled")
            || string.IsNullOrWhiteSpace(_configuration["Email:Host"]))
        {
            return;
        }

        try
        {
            await _emailSender.SendAsync(
                cart.User.Email,
                "Giỏ hàng của bạn đang chờ này!",
                BuildEmailBody(cart, voucher.Code, voucher.EndDate));
            reminder.EmailSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send abandoned cart email for cart {CartId}", cart.Id);
        }
    }

    private async Task<Voucher> CreateRecoveryVoucherAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        string code;
        do
        {
            code = $"CART{RandomNumberGenerator.GetInt32(100000, 999999)}";
        }
        while (await _db.Vouchers.AnyAsync(row => row.Code == code, cancellationToken));

        var voucher = new Voucher
        {
            Code = code,
            Type = VoucherType.FixedAmount,
            Value = RecoveryDiscount,
            MinOrderAmount = MinOrderAmount,
            MaxDiscount = RecoveryDiscount,
            UsageLimit = 1,
            StartDate = now,
            EndDate = now.Add(VoucherLifetime),
            IsActive = true,
            TargetUserId = userId
        };

        _db.Vouchers.Add(voucher);
        return voucher;
    }

    private string BuildEmailBody(Cart cart, string voucherCode, DateTime expiresAt)
    {
        var cartUrl = $"{(_configuration["App:BaseUrl"] ?? _configuration["Email:FrontendUrl"] ?? "http://localhost:5009").TrimEnd('/')}/Cart";
        var itemRows = string.Join("", cart.Items.Take(5).Select(item =>
        {
            var productName = WebUtility.HtmlEncode(item.Product?.Name ?? "Sản phẩm");
            var price = item.Product?.SalePrice ?? item.Product?.Price ?? 0;
            return $"<tr><td style=\"padding:8px 0;border-bottom:1px solid #e5e7eb\">{productName}</td><td style=\"padding:8px 0;text-align:right;border-bottom:1px solid #e5e7eb\">x{item.Quantity}</td><td style=\"padding:8px 0;text-align:right;border-bottom:1px solid #e5e7eb\">{(price * item.Quantity):N0} ₫</td></tr>";
        }));

        var total = cart.Items.Sum(item => item.Quantity * (item.Product?.SalePrice ?? item.Product?.Price ?? 0));
        var html = new StringBuilder();
        html.Append("<div style=\"font-family:Inter,Arial,sans-serif;color:#111827;line-height:1.55;max-width:620px;margin:auto\">");
        html.Append("<h2 style=\"margin:0 0 12px\">Giỏ hàng của bạn đang chờ này!</h2>");
        html.Append("<p>Bạn còn vài sản phẩm trong giỏ. Techvora tặng bạn mã giảm 30.000 ₫ cho đơn từ 300.000 ₫, dùng một lần trong 48 giờ.</p>");
        html.Append("<table style=\"width:100%;border-collapse:collapse;margin:16px 0\">");
        html.Append(itemRows);
        html.Append($"<tr><td colspan=\"2\" style=\"padding:10px 0;font-weight:700\">Tạm tính</td><td style=\"padding:10px 0;text-align:right;font-weight:700\">{total:N0} ₫</td></tr>");
        html.Append("</table>");
        html.Append($"<p>Mã giảm giá: <strong style=\"font-size:18px;letter-spacing:.06em\">{WebUtility.HtmlEncode(voucherCode)}</strong></p>");
        html.Append($"<p>Hạn dùng: <strong>{expiresAt.ToLocalTime():dd/MM/yyyy HH:mm}</strong></p>");
        html.Append($"<p><a href=\"{WebUtility.HtmlEncode(cartUrl)}\" style=\"display:inline-block;background:#0f6bff;color:white;text-decoration:none;padding:12px 16px;border-radius:8px;font-weight:700\">Quay lại giỏ hàng</a></p>");
        html.Append("</div>");
        return html.ToString();
    }

    private async Task<bool> IsAdminUserAsync(string userId, CancellationToken cancellationToken)
    {
        return await _db.UserRoles
            .Join(_db.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .AnyAsync(row => row.UserId == userId && row.Name == "Admin", cancellationToken);
    }
}
