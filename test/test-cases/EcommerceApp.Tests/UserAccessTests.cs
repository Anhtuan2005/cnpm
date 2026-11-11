using EcommerceApp.Data;
using EcommerceApp.Hubs;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace EcommerceApp.Tests;

[Collection("SqlServer")]
public sealed class UserAccessTests(SqlServerFixture fixture)
{
    private const string Reason = "Xác minh tài khoản trong môi trường kiểm thử.";
    private const string Password = "Original123";

    [Fact]
    public async Task Lock_and_unlock_revoke_old_cookies_and_only_new_login_is_accepted()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var admin = await CreateAdminAsync(services);
        var user = await SqlServerFixture.CreateUserAsync(services);
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var factory = new MinimalUserClaimsPrincipalFactory(users, Options.Create(new IdentityOptions()));
        var cookie = await factory.CreateAsync(user);
        var access = Access(services);
        Assert.False((await access.SetLockAsync(admin.Id, user.Id, true, "  ", AdminLockDuration.Indefinite)).Succeeded);
        Assert.True(await CookieAcceptedAsync(services, cookie));
        Assert.True((await access.SetLockAsync(admin.Id, user.Id, true, Reason, AdminLockDuration.Indefinite)).Succeeded);
        Assert.False(await CookieAcceptedAsync(services, cookie));
        var lockedUser = (await users.FindByIdAsync(user.Id))!;
        Assert.Null(lockedUser.AdminLockUntil);
        Assert.True(lockedUser.IsAdminLocked(DateTimeOffset.UtcNow.AddYears(20)));
        Assert.True((await access.SetLockAsync(admin.Id, user.Id, false, Reason)).Succeeded);
        Assert.False(await CookieAcceptedAsync(services, cookie));
        var currentUser = (await users.FindByIdAsync(user.Id))!;
        Assert.Null(currentUser.AdminLockedAt);
        Assert.True(await CookieAcceptedAsync(services, await factory.CreateAsync(currentUser)));
        var history = await services.GetRequiredService<AppDbContext>().AccountAccessEvents
            .Where(entry => entry.UserId == user.Id).OrderBy(entry => entry.Id).ToListAsync();
        Assert.Equal(new[] { AccountAccessEventKinds.AdminLock, AccountAccessEventKinds.Unlock }, history.Select(entry => entry.Kind));
        Assert.All(history, entry =>
        {
            Assert.Equal(admin.Id, entry.ActorId);
            Assert.Equal(Reason, entry.Reason);
        });
    }

    [Fact]
    public async Task Manual_lock_requires_an_explicit_duration_and_ends_without_reviving_old_sessions()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var admin = await CreateAdminAsync(services);
        var user = await SqlServerFixture.CreateUserAsync(services);
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var factory = new MinimalUserClaimsPrincipalFactory(users, Options.Create(new IdentityOptions()));
        var oldCookie = await factory.CreateAsync(user);
        var access = Access(services);

        Assert.False((await access.SetLockAsync(admin.Id, user.Id, true, Reason)).Succeeded);
        Assert.False((await access.SetLockAsync(admin.Id, user.Id, true, Reason, (AdminLockDuration)999)).Succeeded);
        Assert.True(await CookieAcceptedAsync(services, oldCookie));

        var before = DateTimeOffset.UtcNow;
        Assert.True((await access.SetLockAsync(admin.Id, user.Id, true, Reason, AdminLockDuration.OneWeek)).Succeeded);
        var locked = await db.Users.AsNoTracking().SingleAsync(account => account.Id == user.Id);
        var until = Assert.IsType<DateTimeOffset>(locked.AdminLockUntil);
        Assert.InRange(until, before.AddDays(7), DateTimeOffset.UtcNow.AddDays(7));
        Assert.Equal(until, locked.LockoutEnd);
        Assert.False(await CookieAcceptedAsync(services, oldCookie));
        Assert.False((await access.SetAdminAsync(admin.Id, user.Id, true, Reason, Password)).Succeeded);
        var audit = Assert.Single(await db.AccountAccessEvents.Where(entry => entry.UserId == user.Id).ToListAsync());
        Assert.Equal(until, audit.AdminLockUntil);

        var past = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.Users.Where(account => account.Id == user.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(account => account.AdminLockUntil, past)
            .SetProperty(account => account.LockoutEnd, past));
        var expired = await db.Users.AsNoTracking().SingleAsync(account => account.Id == user.Id);
        Assert.False(await CookieAcceptedAsync(services, oldCookie));
        Assert.True(await CookieAcceptedAsync(services, await factory.CreateAsync(expired)));
        Assert.True((await access.SetAdminAsync(admin.Id, user.Id, true, Reason, Password)).Succeeded);
    }

    [Fact]
    public async Task Lockout_invalidates_current_session_even_when_the_stamp_has_not_changed()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var user = await SqlServerFixture.CreateUserAsync(services);
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        await users.SetLockoutEnabledAsync(user, true);
        var cookie = await new MinimalUserClaimsPrincipalFactory(users, Options.Create(new IdentityOptions())).CreateAsync(user);
        await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
        Assert.False(await CookieAcceptedAsync(services, cookie));
    }

    [Fact]
    public async Task Admin_cannot_lock_or_demote_self_and_cannot_act_after_losing_role()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var first = await CreateAdminAsync(services);
        var second = await CreateAdminAsync(services);
        var access = Access(services);
        Assert.False((await access.SetLockAsync(first.Id, first.Id, true, Reason, AdminLockDuration.Indefinite)).Succeeded);
        Assert.False((await access.SetAdminAsync(first.Id, first.Id, false, Reason, Password)).Succeeded);
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var oldCookie = await new MinimalUserClaimsPrincipalFactory(users, Options.Create(new IdentityOptions())).CreateAsync(first);
        Assert.True((await access.SetAdminAsync(second.Id, first.Id, false, Reason, Password)).Succeeded);
        Assert.False(await CookieAcceptedAsync(services, oldCookie));
        Assert.False((await access.SetAdminAsync(first.Id, second.Id, false, Reason, Password)).Succeeded);
        Assert.True(await users.IsInRoleAsync((await users.FindByIdAsync(second.Id))!, "Admin"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_admins_cannot_revoke_each_other_concurrently(bool lockAccounts)
    {
        string firstId, secondId;
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            firstId = (await CreateAdminAsync(setup.ServiceProvider)).Id;
            secondId = (await CreateAdminAsync(setup.ServiceProvider)).Id;
        }
        async Task<bool> RevokeAsync(string actor, string target)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            var access = Access(scope.ServiceProvider);
            return (lockAccounts
                ? await access.SetLockAsync(actor, target, true, Reason, AdminLockDuration.Indefinite)
                : await access.SetAdminAsync(actor, target, false, Reason, Password)).Succeeded;
        }
        Assert.Single(await Task.WhenAll(RevokeAsync(firstId, secondId), RevokeAsync(secondId, firstId)), succeeded => succeeded);
    }

    [Fact]
    public async Task Changing_admin_role_requires_the_actors_password_and_records_only_successful_changes()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var admin = await CreateAdminAsync(services);
        var user = await SqlServerFixture.CreateUserAsync(services);
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await users.ChangePasswordAsync(user, Password, "OtherPassword123")).Succeeded);
        var access = Access(services);
        var factory = new MinimalUserClaimsPrincipalFactory(users, Options.Create(new IdentityOptions()));
        var oldCookie = await factory.CreateAsync(user);

        Assert.False((await access.SetAdminAsync(admin.Id, user.Id, true, null, Password)).Succeeded);
        Assert.False((await access.SetAdminAsync(admin.Id, user.Id, true, Reason, null)).Succeeded);
        Assert.False((await access.SetAdminAsync(admin.Id, user.Id, true, Reason, "OtherPassword123")).Succeeded);
        Assert.True(await CookieAcceptedAsync(services, oldCookie));

        Assert.True((await access.SetAdminAsync(admin.Id, user.Id, true, Reason, Password)).Succeeded);
        Assert.False(await CookieAcceptedAsync(services, oldCookie));
        var promoted = (await users.FindByIdAsync(user.Id))!;
        Assert.True(await users.IsInRoleAsync(promoted, "Admin"));
        var promotedCookie = await factory.CreateAsync(promoted);

        Assert.False((await access.SetAdminAsync(admin.Id, user.Id, false, Reason, "incorrect")).Succeeded);
        Assert.True(await CookieAcceptedAsync(services, promotedCookie));
        Assert.True((await access.SetAdminAsync(admin.Id, user.Id, false, Reason, Password)).Succeeded);
        Assert.False(await CookieAcceptedAsync(services, promotedCookie));
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync(user.Id))!, "Admin"));

        var history = await services.GetRequiredService<AppDbContext>().AccountAccessEvents
            .Where(entry => entry.UserId == user.Id).OrderBy(entry => entry.Id).ToListAsync();
        Assert.Equal(new[] { AccountAccessEventKinds.GrantAdmin, AccountAccessEventKinds.RevokeAdmin }, history.Select(entry => entry.Kind));
        Assert.All(history, entry => Assert.Equal(Reason, entry.Reason));
    }

    [Fact]
    public async Task Login_lockout_is_recorded_once_in_account_history_and_stays_out_of_the_general_inbox()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var admin = await CreateAdminAsync(services);
        var user = await SqlServerFixture.CreateUserAsync(services);
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var notifications = new UserNotificationService(db, users, Mock.Of<IHubContext<AdminNotificationHub>>(),
            NullLogger<UserNotificationService>.Instance);
        var lockouts = new AccountLoginLockoutRecorder(db);
        var end = DateTimeOffset.UtcNow.AddMinutes(15);
        Assert.True((await users.SetLockoutEndDateAsync(user, end)).Succeeded);

        await lockouts.RecordAsync(user.Id, end, 5);
        await lockouts.RecordAsync(user.Id, end, 5);

        var recorded = Assert.Single(await db.AccountAccessEvents.Where(entry => entry.UserId == user.Id).ToListAsync());
        Assert.Equal(AccountAccessEventKinds.LoginLockout, recorded.Kind);
        Assert.Equal(end, recorded.TemporaryLockUntil);
        Assert.False(await db.UserNotifications.AnyAsync(entry => entry.UserId == admin.Id || entry.UserId == user.Id));
        Assert.Null((await users.FindByIdAsync(user.Id))!.AdminLockedAt);
        Assert.True((await Access(services).SetLockAsync(admin.Id, user.Id, true, Reason, AdminLockDuration.Indefinite)).Succeeded);
        Assert.NotNull((await users.FindByIdAsync(user.Id))!.AdminLockedAt);

        var link = $"/Admin/User/Review/{user.Id}";
        await notifications.CreateAsync(admin.Id, "Cảnh báo đăng nhập bất thường", "Cảnh báo cũ", NotificationTypes.System, link);
        await notifications.CreateAsync(admin.Id, "Đơn hàng mới", "Một đơn mẫu", NotificationTypes.Order, "/Admin/Order/1");
        var legacy = await db.UserNotifications.SingleAsync(entry => entry.UserId == admin.Id && entry.LinkUrl == link);
        Assert.Equal(1, await notifications.GetUnreadCountAsync(admin.Id));
        Assert.Equal("Đơn hàng mới", Assert.Single(await notifications.GetAllAsync(admin.Id)).Title);
        Assert.Null(await notifications.GetAsync(legacy.Id, admin.Id));
        await notifications.MarkAllAsReadAsync(admin.Id);
        Assert.Equal(0, await notifications.GetUnreadCountAsync(admin.Id));
        Assert.False((await db.UserNotifications.AsNoTracking().SingleAsync(entry => entry.Id == legacy.Id)).IsRead);
    }

    private static AdminUserAccessService Access(IServiceProvider services) => new(
        services.GetRequiredService<AppDbContext>(), services.GetRequiredService<UserManager<ApplicationUser>>());

    private static async Task<ApplicationUser> CreateAdminAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin")) Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
        var user = await SqlServerFixture.CreateUserAsync(services);
        Assert.True((await services.GetRequiredService<UserManager<ApplicationUser>>().AddToRoleAsync(user, "Admin")).Succeeded);
        return user;
    }

    private static async Task<bool> CookieAcceptedAsync(IServiceProvider services, ClaimsPrincipal principal)
    {
        services.GetRequiredService<AppDbContext>().ChangeTracker.Clear();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var options = Options.Create(new IdentityOptions());
        var signIn = new SignInManager<ApplicationUser>(users, new HttpContextAccessor(), new MinimalUserClaimsPrincipalFactory(users, options),
            options, NullLogger<SignInManager<ApplicationUser>>.Instance,
            new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())), new DefaultUserConfirmation<ApplicationUser>());
        var authentication = new Mock<IAuthenticationService>();
        var stampValidator = new Mock<ISecurityStampValidator>();
        using var requestServices = new ServiceCollection().AddSingleton(authentication.Object).AddSingleton(stampValidator.Object).BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = requestServices };
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow }, scheme.Name);
        var context = new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
        await new ApplicationCookieEvents(signIn, users).ValidatePrincipal(context);
        if (context.Principal is null)
            authentication.Verify(a => a.SignOutAsync(httpContext, IdentityConstants.ApplicationScheme, null), Times.Once());
        else
            stampValidator.Verify(v => v.ValidateAsync(context), Times.Once());
        return context.Principal is not null;
    }
}
