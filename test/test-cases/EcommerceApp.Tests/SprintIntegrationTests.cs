using System.Security.Claims;
using System.Text.Json;
using EcommerceApp.Controllers;
using EcommerceApp.Controllers.Admin;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Sprint_one_cart_flows_into_sprint_two_voucher_delivery_review_and_support()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var customer = await SqlServerFixture.CreateUserAsync(services);
        var admin = await SqlServerFixture.CreateUserAsync(services);
        var outsider = await SqlServerFixture.CreateUserAsync(services);
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin"))
            Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
        Assert.True((await services.GetRequiredService<UserManager<ApplicationUser>>()
            .AddToRoleAsync(admin, "Admin")).Succeeded);

        var category = new Category { Name = "Điện thoại", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var phone = new Product { Name = "Integration phone", Description = "Fixture", Price = 1_000_000, Stock = 5, Category = category };
        var unselected = new Product { Name = "Unselected phone", Description = "Fixture", Price = 2_000_000, Stock = 3, Category = category };
        var voucher = new Voucher
        {
            Code = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(),
            Type = VoucherType.FixedAmount, Value = 100_000, UsageLimit = 1,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1)
        };
        db.Products.AddRange(phone, unselected);
        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync();

        var sprint = new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 2 }));
        var catalog = new PhoneCatalogScope();
        var crossSell = new CrossSellService(db);
        var cart = new CartService(db, crossSell, sprint, catalog);
        var notifications = new UserNotificationService(db, services.GetRequiredService<UserManager<ApplicationUser>>(),
            Hub(), NullLogger<UserNotificationService>.Instance);
        var orders = new OrderService(db, cart, new ShippingFeeService(), Mock.Of<IOrderEmailService>(),
            Mock.Of<ICustomerSegmentService>(), crossSell, notifications, Hub(),
            NullLogger<OrderService>.Instance, sprint, catalog);
        var guestSession = Guid.NewGuid().ToString("N");

        // Sprint 1: merge a guest cart with the customer's existing cart.
        await cart.AddAsync(phone.Id, 1, null, guestSession);
        await cart.AddAsync(phone.Id, 1, customer.Id, guestSession);
        await cart.AddAsync(unselected.Id, 1, customer.Id, guestSession);
        await cart.MergeGuestCartAsync(customer.Id, guestSession);
        var merged = await cart.GetCartAsync(customer.Id, guestSession);
        Assert.Equal(2, merged.Items.Single(item => item.ProductId == phone.Id).Quantity);
        Assert.False(merged.PromotionsEnabled);

        // Sprint 2 extends the same checkout. Client-supplied money must be ignored.
        var checkout = Checkout(phone.Id);
        checkout.Ward = "Phường thử nghiệm";
        checkout.Street = "123 Đường thử nghiệm";
        checkout.VoucherCode = voucher.Code;
        checkout.DiscountAmount = 999_999;
        checkout.ShippingFee = 1;
        var order = await orders.CreateOrderAsync(customer.Id, checkout, guestSession);
        Assert.Equal(1_920_000m, order.TotalAmount);
        Assert.Equal(100_000m, order.DiscountAmount);
        Assert.Equal(20_000m, order.ShippingFee);
        Assert.Equal(unselected.Id, Assert.Single((await cart.GetCartAsync(customer.Id, guestSession)).Items).ProductId);
        Assert.Equal(order.Id, Assert.Single((await orders.GetUserOrderHistoryAsync(customer.Id, 1, 10)).Orders).Id);
        Assert.Null(await orders.GetUserOrderAsync(order.Id, outsider.Id));

        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Delivered));
        var assurance = new OrderAssuranceService(db, notifications);
        Assert.True(await assurance.SetDeliveryReceiptAsync(order.Id, customer.Id, true, null));

        var customerContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, customer.Id), new Claim(ClaimTypes.Role, "User")], "Test"))
        };
        var reviews = new ReviewController(db, Mock.Of<IImageStorageService>())
        {
            ControllerContext = new ControllerContext { HttpContext = customerContext }
        };
        var reviewResult = Assert.IsType<JsonResult>(await reviews.Submit(phone.Id, 5, "Máy hoạt động tốt sau khi nhận hàng.", null));
        Assert.True(JsonSerializer.SerializeToElement(reviewResult.Value).GetProperty("success").GetBoolean());
        var review = await db.Reviews.SingleAsync(row => row.UserId == customer.Id && row.ProductId == phone.Id);
        var adminContext = new DefaultHttpContext();
        var adminReviews = new AdminReviewController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = adminContext },
            TempData = new TempDataDictionary(adminContext, Mock.Of<ITempDataProvider>())
        };
        Assert.IsType<RedirectToActionResult>(await adminReviews.Reply(review.Id, "Shop cảm ơn bạn."));

        // The request and both notification links must refer to this exact order.
        var support = new ReturnWarrantyRequestService(db, Mock.Of<IImageStorageService>(), notifications,
            NullLogger<ReturnWarrantyRequestService>.Instance);
        var requestInput = await support.BuildCreateModelAsync(order.Id, customer.Id);
        Assert.NotNull(requestInput);
        requestInput!.Type = ReturnWarrantyRequestTypes.Warranty;
        requestInput.Reason = "Kiểm tra pin";
        requestInput.Description = "Nhờ shop kiểm tra thời lượng pin sau khi sử dụng.";
        var request = await support.CreateAsync(customer.Id, requestInput);
        Assert.Null(await support.GetUserRequestAsync(request.Id, outsider.Id));

        // Verify persistence from a fresh context, not just tracked entities.
        await using var verification = fixture.Services.CreateAsyncScope();
        var saved = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedOrder = await saved.Orders.SingleAsync(row => row.Id == order.Id);
        Assert.Equal(OrderStatuses.Delivered, savedOrder.Status);
        Assert.True(savedOrder.IsPaid);
        Assert.Equal(ReceiptStatuses.Confirmed, savedOrder.CustomerReceiptStatus);
        Assert.Equal(3, await saved.Products.Where(row => row.Id == phone.Id).Select(row => row.Stock).SingleAsync());
        Assert.Equal(1, await saved.Vouchers.Where(row => row.Id == voucher.Id).Select(row => row.UsedCount).SingleAsync());
        Assert.Single(await saved.VoucherUsages.Where(row => row.OrderId == order.Id).ToListAsync());
        Assert.Equal(-2, (await saved.StockLogs.SingleAsync(row => row.OrderId == order.Id)).ChangeAmount);
        Assert.Equal("Shop cảm ơn bạn.", (await saved.Reviews.SingleAsync(row => row.Id == review.Id)).StoreReply);
        Assert.True(await saved.UserNotifications.AnyAsync(row => row.UserId == customer.Id
            && row.LinkUrl == $"/ReturnWarranty/Details/{request.Id}"));
        Assert.True(await saved.UserNotifications.AnyAsync(row => row.UserId == admin.Id
            && row.LinkUrl == $"/Admin/Order/{order.Id}#support-request-{request.Id}"));
    }
}
