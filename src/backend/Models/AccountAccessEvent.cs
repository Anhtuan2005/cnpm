using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public sealed class AccountAccessEvent
{
    public int Id { get; set; }
    [StringLength(450)] public string UserId { get; set; } = string.Empty;
    [StringLength(450)] public string? ActorId { get; set; }
    [StringLength(120)] public string? ActorName { get; set; }
    [StringLength(24)] public string Kind { get; set; } = string.Empty;
    [StringLength(400)] public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TemporaryLockUntil { get; set; }
    public DateTimeOffset? AdminLockUntil { get; set; }
}

public static class AccountAccessEventKinds
{
    public const string LoginLockout = "LoginLockout";
    public const string AdminLock = "AdminLock";
    public const string Unlock = "Unlock";
    public const string GrantAdmin = "GrantAdmin";
    public const string RevokeAdmin = "RevokeAdmin";

    public static string Label(string kind) => kind switch
    {
        LoginLockout => "Khoá tạm do đăng nhập sai",
        AdminLock => "Khoá thủ công",
        Unlock => "Mở khoá",
        GrantAdmin => "Cấp Admin",
        RevokeAdmin => "Gỡ Admin",
        _ => kind
    };
}
