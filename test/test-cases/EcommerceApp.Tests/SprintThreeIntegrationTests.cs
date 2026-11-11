using System.Net;
using System.Text;
using EcommerceApp.Controllers;
using EcommerceApp.Controllers.Admin;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Sprint_three_admin_voucher_cross_sell_checkout_and_support_form_one_persisted_flow()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var customer = await SqlServerFixture.CreateUserAsync(services);
        var segments = new CustomerSegmentService(db);
        await segments.RefreshUserAsync(customer.Id);
        var newCustomer = await db.CustomerSegments.SingleAsync(row => row.Code == CustomerSegmentCodes.NewCustomer);
        var context = new DefaultHttpContext();
        var vouchers = new AdminVoucherController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
        var voucher = new Voucher
        {
            Code = "S3" + Guid.NewGuid().ToString("N")[..20], Type = VoucherType.FixedAmount,
            Value = 100_000, UsageLimit = 1, StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1), CustomerSegmentId = newCustomer.Id
        };
        Assert.IsType<RedirectToActionResult>(await vouchers.Create(voucher));

        var category = new Category { Name = "Sprint 3 phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var anchor = new Product { Name = "Main phone", Description = "Fixture", Category = category, Price = 1_000_000, DiscountPercent = 10, Stock = 4 };
        var addon = new Product { Name = "Compatible accessory", Description = "Fixture",
            Category = new Category { Name = "Phone accessories", Slug = PhoneCatalogScope.AccessoryRootSlug + "-" + Guid.NewGuid().ToString("N") },
            Price = 500_000, Stock = 4 };
        db.Products.AddRange(anchor, addon);
        db.CrossSellOffers.Add(new CrossSellOffer { AnchorProduct = anchor, AddOnProduct = addon, DiscountPercent = 10, IsActive = true });
        await db.SaveChangesAsync();
        var features = new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 3 }));
        var catalog = new PhoneCatalogScope();
        var crossSell = new CrossSellService(db, catalog);
        var cart = new CartService(db, crossSell, features, catalog);
        await cart.AddAsync(anchor.Id, 1, customer.Id, "sprint3");
        var suggestions = (await cart.GetCartAsync(customer.Id, "sprint3")).CrossSellSuggestions;
        Assert.Contains(suggestions, item => item.ProductId == addon.Id && item.OfferPrice == 450_000);
        await cart.AddAsync(addon.Id, 1, customer.Id, "sprint3");
        var notifications = new UserNotificationService(db, services.GetRequiredService<UserManager<ApplicationUser>>(), Hub(), NullLogger<UserNotificationService>.Instance);
        var orders = new OrderService(db, cart, new ShippingFeeService(), Mock.Of<IOrderEmailService>(), segments,
            crossSell, notifications, Hub(), NullLogger<OrderService>.Instance, features, catalog);
        var checkout = Checkout(anchor.Id);
        checkout.SelectedProductIds.Add(addon.Id);
        checkout.VoucherCode = voucher.Code;
        checkout.DiscountAmount = 999_999;
        var order = await orders.CreateOrderAsync(customer.Id, checkout, "sprint3");
        Assert.Equal(1_270_000m, order.TotalAmount); // 900,000 + 450,000 - 100,000 + 20,000
        Assert.Equal(450_000m, order.Items.Single(item => item.ProductId == addon.Id).UnitPrice);
        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
        Assert.True(await orders.UpdateStatusAsync(order.Id, OrderStatuses.Delivered));

        var support = new ReturnWarrantyRequestService(db, Mock.Of<IImageStorageService>(), notifications, NullLogger<ReturnWarrantyRequestService>.Instance);
        var input = SupportInput(order.Id, order.Items.First().Id);
        input.Type = ReturnWarrantyRequestTypes.Warranty;
        var request = await support.CreateAsync(customer.Id, input);
        var admin = new AdminReturnWarrantyController(support)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
        Assert.IsType<RedirectToActionResult>(await admin.UpdateStatus(request.Id, ReturnWarrantyRequestStatuses.Reviewing, "Đã tiếp nhận"));
        await admin.UpdateStatus(request.Id, ReturnWarrantyRequestStatuses.Approved, "Đã kiểm tra");
        await admin.UpdateStatus(request.Id, ReturnWarrantyRequestStatuses.Completed, "Đã bảo hành xong");
        var notificationCount = await db.UserNotifications.CountAsync(row => row.UserId == customer.Id && row.Type == NotificationTypes.Support);
        await admin.UpdateStatus(request.Id, ReturnWarrantyRequestStatuses.Completed, "Lặp");
        Assert.Equal(notificationCount, await db.UserNotifications.CountAsync(row => row.UserId == customer.Id && row.Type == NotificationTypes.Support));
        db.ChangeTracker.Clear();
        var saved = await support.GetUserRequestAsync(request.Id, customer.Id);
        Assert.Equal("Đã bảo hành xong", saved!.AdminNote);
        Assert.Equal(ReturnWarrantyRequestStatuses.Completed, saved.Status);
        Assert.Null(await support.GetUserRequestAsync(request.Id, "another-user"));
        Assert.Single(await db.VoucherUsages.Where(row => row.OrderId == order.Id).ToListAsync());
        Assert.Equal(1, (await db.Vouchers.FindAsync(voucher.Id))!.UsedCount);
        Assert.Equal(RefundStatuses.NotRequired, (await db.Orders.FindAsync(order.Id))!.RefundStatus);
    }

    [Fact]
    public async Task Report_and_csv_reconcile_discount_shipping_refunds_and_date_boundaries()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var date = new DateTime(2001, 1, 15);
        var category = new Category { Name = "=Unsafe category", Slug = Guid.NewGuid().ToString("N") };
        var products = new[]
        {
            new Product { Name = "Same display name", Description = "Fixture", Category = category, Price = 1_000_000 },
            new Product { Name = "Same display name", Description = "Fixture", Category = category, Price = 1_000_000 }
        };
        Order Sale(Product product, DateTime time, string status, string refund, int quantity = 1) => new()
        {
            UserId = customer.Id, CreatedAt = time, Status = status, RefundStatus = refund, IsPaid = true,
            RecipientName = "Report test", RecipientPhone = "0900000000", ShippingAddress = "Fixture",
            DiscountAmount = 100_000, ShippingFee = 20_000, TotalAmount = quantity * 1_000_000 - 80_000,
            Items = [new OrderItem { Product = product, Quantity = quantity, UnitPrice = 1_000_000 }]
        };
        db.Orders.AddRange(
            Sale(products[0], date, OrderStatuses.Delivered, RefundStatuses.NotRequired, 2),
            Sale(products[1], date.AddDays(1).AddTicks(-1), OrderStatuses.Confirmed, RefundStatuses.NotRequired),
            Sale(products[0], date.AddDays(1), OrderStatuses.Delivered, RefundStatuses.NotRequired),
            Sale(products[0], date, OrderStatuses.Cancelled, RefundStatuses.NotRequired),
            Sale(products[0], date, OrderStatuses.Delivered, RefundStatuses.PendingManual),
            Sale(products[0], date, OrderStatuses.Delivered, RefundStatuses.Refunded));
        await db.SaveChangesAsync();
        products[0].IsDeleted = true;
        await db.SaveChangesAsync();
        var controller = new AdminReportController(db);
        var report = Assert.IsType<AdminReportViewModel>(Assert.IsType<ViewResult>(await controller.Index("custom", date, date)).Model);
        Assert.Equal(2, report.TotalOrders);
        Assert.Equal(3_000_000m, report.MerchandiseRevenue);
        Assert.Equal(200_000m, report.VoucherDiscount);
        Assert.Equal(40_000m, report.ShippingRevenue);
        Assert.Equal(2_840_000m, report.CurrentPeriodRevenue);
        Assert.Equal(report.CurrentPeriodRevenue, report.DailyRevenue.Sum(row => row.Revenue));
        Assert.Equal(report.MerchandiseRevenue, report.RevenueByCategory.Values.Sum());
        Assert.Equal(2, report.TopProducts.Count); // Distinct IDs with the same name stay separate.
        Assert.Null(report.GrowthPercent);
        var file = Assert.IsType<FileContentResult>(await controller.ExportCsv("custom", date, date));
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("2840000", csv);
        Assert.Contains("\"'=Unsafe category\"", csv);
        Assert.Contains("2001-01-15", csv);
    }

    [Fact]
    public async Task Recommendations_include_old_catalog_fallback_without_using_another_accounts_session_history()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var other = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var product = new Product { Name = "Old available phone", Description = "Fixture", Price = 1_000_000,
            Stock = 2, CreatedAt = DateTime.UtcNow.AddYears(-2), Category = new Category { Name = "Phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") } };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var exclusions = await db.Products.Where(row => row.Id != product.Id).Select(row => row.Id).ToListAsync();
        var service = new RecommendationService(db, new PhoneCatalogScope());
        Assert.Equal(product.Id, Assert.Single(await service.GetRecommendationsAsync(null, null, excludedProductIds: exclusions)).Id);
        db.ProductInteractions.Add(new ProductInteraction { ProductId = product.Id, UserId = other.Id, SessionId = "shared-browser" });
        db.Carts.Add(new Cart { UserId = other.Id, SessionId = "shared-browser", Items = [new CartItem { ProductId = product.Id, Quantity = 1 }] });
        await db.SaveChangesAsync();
        Assert.Equal(product.Id, Assert.Single(await service.GetRecommendationsAsync(user.Id, "shared-browser", excludedProductIds: exclusions)).Id);
        Assert.Equal(product.Id, Assert.Single(await service.GetRecommendationsAsync(null, "shared-browser", excludedProductIds: exclusions)).Id);
    }

    [Fact]
    public async Task Recommendations_keep_saved_phones_and_rank_brand_and_price_ahead_of_popularity()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var other = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var category = new Category { Name = "Phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        Product Phone(string name, decimal price, int stock = 2) => new()
        {
            Name = name, Description = "Fixture", Price = price, Stock = stock, Category = category
        };
        var saved = Phone("Samsung Galaxy S24", 22_000_000);
        var sameBrand = Phone("Samsung Galaxy A55", 10_000_000);
        var similarPrice = Phone("Xiaomi 14", 18_000_000);
        var popular = Phone("iPhone SE", 8_000_000);
        popular.IsFeatured = true;
        var unavailable = Phone("Samsung Galaxy S24 Plus", 22_000_000, stock: 0);
        var phones = new[] { saved, sameBrand, similarPrice, popular, unavailable };
        db.Products.AddRange(phones);
        await db.SaveChangesAsync();

        var included = phones.Select(product => product.Id).ToList();
        var exclusions = await db.Products.Where(product => !included.Contains(product.Id)).Select(product => product.Id).ToListAsync();
        var service = new RecommendationService(db, new PhoneCatalogScope());
        var cold = await service.GetRecommendationsAsync(customer.Id, "shared-browser", excludedProductIds: exclusions);
        Assert.Equal(popular.Id, cold[0].Id);

        db.ProductInteractions.Add(new ProductInteraction { ProductId = saved.Id, UserId = customer.Id, SessionId = "shared-browser" });
        db.WishlistItems.AddRange(
            new WishlistItem { ProductId = saved.Id, UserId = customer.Id },
            new WishlistItem { ProductId = popular.Id, UserId = other.Id });
        await db.SaveChangesAsync();

        var personalized = await service.GetRecommendationsAsync(customer.Id, "shared-browser", excludedProductIds: exclusions);
        Assert.Equal(new[] { saved.Id, sameBrand.Id, similarPrice.Id, popular.Id }, personalized.Select(product => product.Id));
        var otherResults = await service.GetRecommendationsAsync(other.Id, "shared-browser", excludedProductIds: exclusions);
        Assert.Equal(popular.Id, otherResults[0].Id);

        var onDetail = await service.GetRecommendationsAsync(customer.Id, "shared-browser",
            anchorProductId: saved.Id, excludedProductIds: exclusions);
        Assert.Equal(new[] { sameBrand.Id, similarPrice.Id, popular.Id }, onDetail.Select(product => product.Id));
    }

    [Fact]
    public async Task Abandoned_cart_scan_creates_one_private_offer_and_notification_per_cart_version_without_fake_email_delivery()
    {
        var (productId, users) = await SeedCartsAsync(5, 1);
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<AppDbContext>();
            var cart = await db.Carts.SingleAsync(row => row.UserId == users[0]);
            cart.UpdatedAt = DateTime.UtcNow.AddDays(-2);
            await db.SaveChangesAsync();
        }
        var email = new Mock<IEmailSender>();
        async Task Scan()
        {
            await using var run = fixture.Services.CreateAsyncScope();
            await Recovery(run.ServiceProvider.GetRequiredService<AppDbContext>(), email.Object).ProcessDueCartsAsync();
        }
        await Task.WhenAll(Scan(), Scan());
        await Scan();
        await using var verify = fixture.Services.CreateAsyncScope();
        var saved = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var reminder = Assert.Single(await saved.AbandonedCartReminders.Include(row => row.Voucher).Where(row => row.UserId == users[0]).ToListAsync());
        Assert.Null(reminder.EmailSentAt);
        Assert.Equal(users[0], reminder.Voucher!.TargetUserId);
        Assert.Equal(30_000m, reminder.Voucher.Value);
        Assert.Single(await saved.UserNotifications.Where(row => row.UserId == users[0] && row.Type == NotificationTypes.Voucher).ToListAsync());
        email.Verify(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var service = Recovery(saved, email.Object);
        Assert.NotNull(await service.GetLoginReminderAsync(users[0]));
        Assert.Null(await service.GetLoginReminderAsync(users[0]));
        reminder.LastShownAt = null;
        reminder.Voucher.UsedCount = 1;
        await saved.SaveChangesAsync();
        Assert.Null(await service.GetLoginReminderAsync(users[0]));
    }

    [Fact]
    public async Task Recovery_batches_move_past_processed_carts()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var product = new Product { Name = "Batch phone", Description = "Fixture", Stock = 100, Price = 500_000,
            Category = new Category { Name = "Batch", Slug = Guid.NewGuid().ToString("N") } };
        var carts = Enumerable.Range(0, 51).Select(index => new Cart { UserId = user.Id, UpdatedAt = DateTime.UtcNow.AddDays(-4).AddSeconds(index),
            Items = [new CartItem { Product = product, Quantity = 1 }] }).ToList();
        db.Carts.AddRange(carts);
        await db.SaveChangesAsync();
        var service = Recovery(db, Mock.Of<IEmailSender>());
        await service.ProcessDueCartsAsync();
        await service.ProcessDueCartsAsync();
        Assert.Equal(51, await db.AbandonedCartReminders.CountAsync(row => row.UserId == user.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Chat_falls_back_to_real_in_stock_phones_with_brand_and_budget_filters(bool providerFails)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new Category { Name = "Samsung", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var phone = new Product { Name = "Samsung Sprint3 " + Guid.NewGuid().ToString("N"), Description = "Fixture", Category = category, Price = 8_000_000, Stock = 2, IsFeatured = true };
        db.Products.AddRange(phone,
            new Product { Name = "Samsung unavailable", Description = "Fixture", Category = category, Price = 5_000_000, Stock = 0 },
            new Product { Name = "Samsung expensive", Description = "Fixture", Category = category, Price = 20_000_000, Stock = 2 },
            new Product { Name = "Samsung accessory", Description = "Fixture", Category = new Category { Name = "Accessories", Slug = Guid.NewGuid().ToString("N") }, Price = 1_000_000, Stock = 2 });
        await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(providerFails
            ? new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-only" } : []).Build();
        using var handler = new FailingChatHandler();
        using var http = new HttpClient(handler);
        var chat = new GeminiChatService(http, db, config, NullLogger<GeminiChatService>.Instance, new PhoneCatalogScope());
        var answer = await chat.AskAsync("Samsung dưới 10 triệu", []);
        Assert.Contains(answer.Products, row => row.Id == phone.Id);
        Assert.All(answer.Products, row => Assert.DoesNotContain("unavailable", row.Name));
        Assert.All(answer.Products, row => Assert.DoesNotContain("expensive", row.Name));
        Assert.All(answer.Products, row => Assert.DoesNotContain("accessory", row.Name));
        Assert.DoesNotContain("API", answer.Reply);
        Assert.Equal(providerFails ? 1 : 0, handler.Calls);
        Assert.Empty((await chat.AskAsync("Samsung dưới 0,01 triệu", [])).Products);
    }

    private static AbandonedCartRecoveryService Recovery(AppDbContext db, IEmailSender email) => new(db, email,
        new ConfigurationBuilder().Build(), NullLogger<AbandonedCartRecoveryService>.Instance);

    private sealed class FailingChatHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("private provider error") });
        }
    }
}
