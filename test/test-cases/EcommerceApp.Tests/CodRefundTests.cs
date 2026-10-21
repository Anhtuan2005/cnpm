using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("manual-shipping")]
    [InlineData("ghn")]
    public async Task Cod_delivery_records_collection_through_every_delivery_path(string path)
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        await using var scope = fixture.Services.CreateAsyncScope();
        var service = CreateService(scope.ServiceProvider);
        var order = await service.CreateOrderAsync(users[0], Checkout(productId), "test");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Orders.Where(row => row.Id == order.Id).Select(row => row.IsPaid).SingleAsync());
        Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        if (path == "admin")
        {
            Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
            Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Delivered));
        }
        else if (path == "manual-shipping")
        {
            var shipping = new ShippingService(db, Mock.Of<IOrderEmailService>(), Mock.Of<IUserNotificationService>());
            Assert.True(await shipping.AssignAsync(order.Id, ShippingCarriers.Ghn, "COD-" + order.Id, null));
            Assert.True(await shipping.UpdateStatusAsync(order.Id, ShippingStatuses.Delivered));
        }
        else
        {
            await Ghn(db).ApplyWebhookAsync(new GhnWebhookPayload
            {
                ClientOrderCode = $"DH{order.Id}", OrderCode = "COD-" + order.Id, Status = "delivered"
            });
        }
        db.ChangeTracker.Clear();
        var saved = await db.Orders.SingleAsync(row => row.Id == order.Id);
        Assert.Equal(OrderStatuses.Delivered, saved.Status);
        Assert.True(saved.IsPaid);
        Assert.NotNull(saved.PaidAt);
        Assert.Equal(RefundStatuses.NotRequired, saved.RefundStatus);
    }

    [Fact]
    public async Task Delivered_cod_refund_requires_reason_and_receipt_and_does_not_restock_or_refund_twice()
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        int orderId;
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            var service = CreateService(setup.ServiceProvider);
            orderId = (await service.CreateOrderAsync(users[0], Checkout(productId), "test")).Id;
            Assert.False(await service.MarkManualRefundCompletedAsync(orderId, ManualRefundMethods.Cash, "COD-RECEIPT", "Chưa giao hàng"));
            await service.UpdateStatusAsync(orderId, OrderStatuses.Confirmed);
            await service.UpdateStatusAsync(orderId, OrderStatuses.Shipping);
            Assert.False(await service.MarkManualRefundCompletedAsync(orderId, ManualRefundMethods.Cash, "COD-RECEIPT", "Chưa thu COD"));
            await service.UpdateStatusAsync(orderId, OrderStatuses.Delivered);
            Assert.False(await service.MarkManualRefundCompletedAsync(orderId, ManualRefundMethods.Vnpay, "COD-RECEIPT", "Hoàn tiền COD"));
            Assert.False(await service.MarkManualRefundCompletedAsync(orderId, ManualRefundMethods.Cash, " ", "Hoàn tiền COD"));
            Assert.False(await service.MarkManualRefundCompletedAsync(orderId, ManualRefundMethods.Cash, "COD-RECEIPT", " "));
        }
        async Task<bool> RecordAsync()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            return await CreateService(scope.ServiceProvider).MarkManualRefundCompletedAsync(orderId,
                ManualRefundMethods.Cash, "COD-RECEIPT", "Đã hoàn tiền sau khi thống nhất với khách hàng.");
        }
        Assert.Single(await Task.WhenAll(RecordAsync(), RecordAsync()), success => success);
        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.SingleAsync(row => row.Id == orderId);
        Assert.True(order.IsPaid);
        Assert.Equal(OrderStatuses.Delivered, order.Status);
        Assert.Equal(RefundStatuses.Refunded, order.RefundStatus);
        Assert.Equal(ManualRefundMethods.Cash, order.RefundMethod);
        Assert.Equal("COD-RECEIPT", order.RefundReference);
        Assert.Equal(order.TotalAmount, order.RefundAmount);
        Assert.False(await db.Orders.WithRecognizedRevenue().AnyAsync(row => row.Id == orderId));
        Assert.Equal(1, await db.Products.Where(product => product.Id == productId).Select(product => product.Stock).SingleAsync());
        Assert.Single(await db.StockLogs.Where(log => log.OrderId == orderId).ToListAsync());
    }
}
