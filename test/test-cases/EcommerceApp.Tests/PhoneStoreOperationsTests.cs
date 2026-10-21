using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Admin_phone_specs_survive_saving_and_renaming_without_inventing_missing_values()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new Category { Name = "Điện thoại", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var service = new ProductService(db, new PhoneCatalogScope());
        var model = new ProductFormViewModel
        {
            Name = "Samsung Galaxy S24", Description = "Fixture", CategoryId = category.Id, Price = 10_000_000, Stock = 3,
            Specifications = new() { ["chip"] = "Chip đã xác minh", ["display"] = "Màn hình đúng phiên bản", ["memory"] = "8GB RAM, 256GB lưu trữ" }
        };
        var product = await service.CreateProductAsync(model, Array.Empty<string>());
        model.Id = product.Id;
        model.RowVersion = Convert.ToBase64String(product.RowVersion);
        model.Name = "Tên điện thoại đã đổi";
        model.Specifications["display"] = " ";
        await service.UpdateProductAsync(model, Array.Empty<string>());

        await using var verification = fixture.Services.CreateAsyncScope();
        var savedDb = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await savedDb.Products.Include(row => row.Category).SingleAsync(row => row.Id == product.Id);
        var specs = new ProductSpecService().Build(saved).ToDictionary(spec => spec.Label, spec => spec.Value);
        Assert.Equal("Chip đã xác minh", specs["Chip xử lý"]);
        Assert.Equal("8GB RAM, 256GB lưu trữ", specs["RAM / Bộ nhớ"]);
        Assert.Equal(PhoneSpecificationFields.Unknown, specs["Màn hình"]);
        Assert.Equal(PhoneSpecificationFields.Unknown, specs["Bảo hành"]);
        var opening = Assert.Single(await savedDb.StockLogs.Where(log => log.ProductId == saved.Id).ToListAsync());
        Assert.Equal(StockMovementTypes.Opening, opening.MovementType);
        Assert.Equal(0, opening.QuantityBefore);
        Assert.Equal(3, opening.QuantityAfter);
    }

    [Fact]
    public async Task Confirmed_customer_order_can_be_cancelled_and_logs_stock_restoration_exactly_once()
    {
        var (productId, users) = await SeedCartsAsync(3, 1);
        int orderId;
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            var service = CreateService(setup.ServiceProvider);
            orderId = (await service.CreateOrderAsync(users[0], Checkout(productId), "test")).Id;
            Assert.True(await service.UpdateStatusAsync(orderId, OrderStatuses.Confirmed));
        }
        async Task<bool> CancelAsync()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return await CreateService(scope.ServiceProvider).CancelUserOrderAsync(orderId, users[0], null);
        }
        Assert.Single(await Task.WhenAll(CancelAsync(), CancelAsync()), cancelled => cancelled);
        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = await db.StockLogs.Where(log => log.OrderId == orderId).OrderBy(log => log.Id).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal(StockMovementTypes.Sale, logs[0].MovementType);
        Assert.Equal(3, logs[0].QuantityBefore);
        Assert.Equal(2, logs[0].QuantityAfter);
        Assert.Equal(StockMovementTypes.Cancellation, logs[1].MovementType);
        Assert.Equal(2, logs[1].QuantityBefore);
        Assert.Equal(3, logs[1].QuantityAfter);
        Assert.All(logs, log => Assert.Equal(users[0], log.ChangedByUserId));
        Assert.Equal(3, await db.Products.Where(product => product.Id == productId).Select(product => product.Stock).SingleAsync());
    }

    [Fact]
    public async Task Customer_cancellation_and_shipping_assignment_cannot_both_win()
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        int orderId;
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            var service = CreateService(setup.ServiceProvider);
            orderId = (await service.CreateOrderAsync(users[0], Checkout(productId), "test")).Id;
            await service.UpdateStatusAsync(orderId, OrderStatuses.Confirmed);
        }
        await using var assigning = fixture.Services.CreateAsyncScope();
        await using var cancelling = fixture.Services.CreateAsyncScope();
        var shipping = new ShippingService(assigning.ServiceProvider.GetRequiredService<AppDbContext>(),
            Mock.Of<IOrderEmailService>(), Mock.Of<IUserNotificationService>());
        Assert.False(await shipping.AssignAsync(orderId, "Viettel Post", "OUT-OF-SCOPE", null));
        var assigned = shipping.AssignAsync(orderId, ShippingCarriers.Ghn, "GHN-TEST", null);
        var cancelled = CreateService(cancelling.ServiceProvider).CancelUserOrderAsync(orderId, users[0], null);
        Assert.Single(await Task.WhenAll(assigned, cancelled), succeeded => succeeded);
        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(await cancelled ? OrderStatuses.Cancelled : OrderStatuses.Shipping,
            await db.Orders.Where(order => order.Id == orderId).Select(order => order.Status).SingleAsync());
        Assert.Equal(await cancelled ? 2 : 1, await db.Products.Where(product => product.Id == productId).Select(product => product.Stock).SingleAsync());
    }
}
