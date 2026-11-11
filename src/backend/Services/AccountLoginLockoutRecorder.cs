using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

public sealed class AccountLoginLockoutRecorder(AppDbContext db)
{
    public async Task RecordAsync(string userId, DateTimeOffset lockedUntil, int failureThreshold)
    {
        db.AccountAccessEvents.Add(new AccountAccessEvent
        {
            UserId = userId,
            Kind = AccountAccessEventKinds.LoginLockout,
            Reason = $"Hệ thống tự khoá tạm sau {failureThreshold} lần đăng nhập thất bại.",
            TemporaryLockUntil = lockedUntil
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            if (!await db.AccountAccessEvents.AnyAsync(entry =>
                entry.UserId == userId && entry.TemporaryLockUntil == lockedUntil))
                throw;
        }
    }
}
