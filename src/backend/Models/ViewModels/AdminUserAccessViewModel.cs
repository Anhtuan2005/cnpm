namespace EcommerceApp.Models.ViewModels;

public sealed record AdminUserIndexViewModel(
    IReadOnlyList<ApplicationUser> Accounts,
    IReadOnlyDictionary<string, IList<string>> Roles);

public sealed record AdminUserAccessViewModel(
    ApplicationUser Account,
    IList<string> Roles,
    IReadOnlyList<AccountAccessEvent> History,
    bool IsSelf,
    bool ConfirmRole)
{
    public bool IsAdmin => Roles.Contains("Admin");
    public bool AdminLocked => LockStatus.IsAdminLock;
    public AdminUserLockStatus LockStatus { get; } = AdminUserLockStatus.From(
        Account.AdminLockedAt, Account.AdminLockUntil, Account.LockoutEnabled, Account.LockoutEnd, DateTimeOffset.UtcNow);
    public bool TemporarilyLocked => LockStatus.TemporaryLockUntil.HasValue;
    public bool IsLocked => LockStatus.IsLocked;
}

public sealed record AdminUserLockStatus(
    string Label, bool IsLocked, DateTimeOffset? TemporaryLockUntil = null, bool IsAdminLock = false)
{
    public static AdminUserLockStatus From(
        DateTimeOffset? adminLockedAt, DateTimeOffset? adminLockUntil, bool lockoutEnabled, DateTimeOffset? lockoutEnd, DateTimeOffset now)
    {
        if (ApplicationUser.IsAdminLockActive(adminLockedAt, adminLockUntil, now))
            return new(adminLockUntil is { } end
                ? $"Quản trị viên đã khoá đến {end.ToLocalTime():HH:mm dd/MM/yyyy}"
                : "Quản trị viên đã khoá không thời hạn", true, IsAdminLock: true);
        if (lockoutEnabled && lockoutEnd is { } until && until > now)
            return new($"Tạm khoá đăng nhập đến {until.ToLocalTime():HH:mm dd/MM/yyyy}", true, until);
        return new("Hoạt động", false);
    }
}
