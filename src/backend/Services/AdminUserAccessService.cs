using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

public sealed record UserAccessResult(bool Succeeded, string Message);

public sealed class AdminUserAccessService(AppDbContext db, UserManager<ApplicationUser> users)
{
    public Task<UserAccessResult> SetLockAsync(string actorId, string userId, bool locked, string? reason, AdminLockDuration? duration = null) =>
        ChangeAsync(actorId, userId, locked, null, reason, null, duration);

    public Task<UserAccessResult> SetAdminAsync(string actorId, string userId, bool admin, string? reason, string? currentPassword) =>
        ChangeAsync(actorId, userId, null, admin, reason, currentPassword, null);

    private async Task<UserAccessResult> ChangeAsync(string actorId, string userId, bool? locked, bool? admin,
        string? reason, string? currentPassword, AdminLockDuration? duration)
    {
        reason = reason?.Trim();
        if (reason is null || reason.Length is < 5 or > 400)
            return new UserAccessResult(false, "Vui lòng nhập lý do từ 5 đến 400 ký tự.");
        if (locked == true && (duration is null || !Enum.IsDefined(duration.Value)))
            return new UserAccessResult(false, "Vui lòng chọn thời hạn khoá tài khoản.");
        if (admin.HasValue && (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 1024))
            return new UserAccessResult(false, "Vui lòng nhập mật khẩu hiện tại của bạn để xác nhận.");

        try
        {
            return await DatabaseTransaction.ExecuteAsync(db, async () =>
            {
                // All privilege changes share this lock; two admins cannot remove each other concurrently.
                var role = await db.Roles.FromSqlRaw("SELECT * FROM AspNetRoles WITH (UPDLOCK, HOLDLOCK) WHERE NormalizedName = N'ADMIN'")
                    .SingleOrDefaultAsync();
                var now = DateTimeOffset.UtcNow;
                var activeAccounts = db.Users.Where(u =>
                    (u.AdminLockedAt == null || (u.AdminLockUntil != null && u.AdminLockUntil <= now)) &&
                    (!u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd <= now));
                var actor = await activeAccounts.SingleOrDefaultAsync(u => u.Id == actorId);
                if (role is null || !await db.UserRoles.AnyAsync(r => r.RoleId == role.Id && r.UserId == actorId) ||
                    actor is null)
                    return new UserAccessResult(false, "Bạn không còn quyền quản trị đang hoạt động. Vui lòng đăng nhập lại.");
                if (actorId == userId && (locked == true || admin == false))
                    return new UserAccessResult(false, "Không thể tự khoá tài khoản hoặc tự gỡ quyền Admin.");
                if (admin.HasValue && !await users.CheckPasswordAsync(actor, currentPassword!))
                    return new UserAccessResult(false, "Mật khẩu xác nhận của quản trị viên không đúng.");

                var user = await db.Users.FromSqlInterpolated($"SELECT * FROM AspNetUsers WITH (UPDLOCK, ROWLOCK) WHERE Id = {userId}")
                    .SingleOrDefaultAsync();
                if (user is null) return new UserAccessResult(false, "Không tìm thấy tài khoản.");
                var isAdmin = await users.IsInRoleAsync(user, "Admin");
                var hasAdminLock = user.IsAdminLocked(now);
                var isLocked = hasAdminLock || (user.LockoutEnabled && user.LockoutEnd > now);
                if (admin == true && isLocked)
                    return new UserAccessResult(false, "Cần mở khoá tài khoản trước khi cấp quyền Admin.");
                if (admin == isAdmin || (locked == true && hasAdminLock) || (locked == false && !isLocked))
                    return new UserAccessResult(false, "Trạng thái tài khoản đã thay đổi. Hãy kiểm tra lại trước khi tiếp tục.");
                if (isAdmin && (locked == true || admin == false) &&
                    !await db.UserRoles.Where(r => r.RoleId == role.Id && r.UserId != userId)
                        .Join(activeAccounts, r => r.UserId, u => u.Id, (r, u) => u)
                        .AnyAsync())
                    return new UserAccessResult(false, "Cần giữ ít nhất một Admin đang hoạt động.");

                DateTimeOffset? adminLockUntil = locked == true && duration is { } selected && selected != AdminLockDuration.Indefinite
                    ? now.AddDays((int)selected)
                    : null;
                if (locked.HasValue)
                {
                    user.AdminLockedAt = locked.Value ? now : null;
                    user.AdminLockUntil = adminLockUntil;
                    EnsureSucceeded(await users.SetLockoutEnabledAsync(user, true));
                    EnsureSucceeded(await users.SetLockoutEndDateAsync(user, locked.Value ? adminLockUntil ?? DateTimeOffset.MaxValue : null));
                    EnsureSucceeded(await users.ResetAccessFailedCountAsync(user));
                }
                else if (admin.HasValue)
                {
                    EnsureSucceeded(admin.Value ? await users.AddToRoleAsync(user, "Admin") : await users.RemoveFromRoleAsync(user, "Admin"));
                }
                // Revoke all previously issued cookies, including those from before an unlock or promotion.
                EnsureSucceeded(await users.UpdateSecurityStampAsync(user));
                db.AccountAccessEvents.Add(new AccountAccessEvent
                {
                    UserId = userId,
                    ActorId = actorId,
                    ActorName = actor.FullName,
                    Kind = locked switch
                    {
                        true => AccountAccessEventKinds.AdminLock,
                        false => AccountAccessEventKinds.Unlock,
                        null => admin == true ? AccountAccessEventKinds.GrantAdmin : AccountAccessEventKinds.RevokeAdmin
                    },
                    Reason = reason,
                    CreatedAt = now,
                    AdminLockUntil = adminLockUntil
                });
                await db.SaveChangesAsync();
                return new UserAccessResult(true, locked.HasValue
                    ? locked.Value ? "Đã khoá tài khoản và thu hồi phiên đăng nhập cũ." : "Đã mở khoá. Người dùng cần đăng nhập lại."
                    : admin == true ? "Đã cấp quyền Admin. Người dùng cần đăng nhập lại." : "Đã gỡ quyền Admin và thu hồi phiên đăng nhập cũ.");
            });
        }
        catch (IdentityAccessUpdateException)
        {
            return new UserAccessResult(false, "Không thể cập nhật tài khoản. Dữ liệu có thể vừa thay đổi; hãy tải lại trang và thử lại.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new IdentityAccessUpdateException();
    }
    private sealed class IdentityAccessUpdateException : Exception;
}
