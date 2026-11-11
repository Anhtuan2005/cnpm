using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }

    public DateTimeOffset? AdminLockedAt { get; set; }
    public DateTimeOffset? AdminLockUntil { get; set; }

    public bool IsAdminLocked(DateTimeOffset now) => IsAdminLockActive(AdminLockedAt, AdminLockUntil, now);

    public static bool IsAdminLockActive(DateTimeOffset? lockedAt, DateTimeOffset? until, DateTimeOffset now) =>
        lockedAt.HasValue && (until is null || until > now);
}
