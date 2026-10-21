using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Customer_delivery_report_is_audited_and_can_be_reopened_only_by_admin()
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await CreateService(scope.ServiceProvider).CreateOrderAsync(users[0], Checkout(productId), "assurance");
        var assurance = new OrderAssuranceService(db, Mock.Of<IUserNotificationService>());

        Assert.False(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], true, null));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Delivered));
        Assert.False(await assurance.RecordDeliveryProofAsync(order.Id, "admin", "http://example.test/proof"));
        Assert.True(await assurance.RecordDeliveryProofAsync(order.Id, "admin", "https://example.test/proof"));
        Assert.False(await assurance.SetDeliveryReceiptAsync(order.Id, "other-user", true, null));
        Assert.False(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], false, " "));
        Assert.True(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], false, "Chưa nhận được kiện hàng"));
        Assert.False(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], true, null));
        Assert.False(await assurance.ResolveDeliveryIssueAsync(order.Id, "admin", " "));
        Assert.True(await assurance.ResolveDeliveryIssueAsync(order.Id, "admin", "Đã kiểm tra mã vận đơn, xin xác nhận lại."));
        Assert.True(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], true, null));
        Assert.False(await assurance.SetDeliveryReceiptAsync(order.Id, users[0], true, null));

        db.ChangeTracker.Clear();
        var saved = await db.Orders.Include(o => o.AssuranceEvents).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(ReceiptStatuses.Confirmed, saved.CustomerReceiptStatus);
        Assert.NotNull(saved.CustomerReceiptConfirmedAt);
        Assert.Equal("https://example.test/proof", saved.DeliveryProofUrl);
        Assert.Equal(4, saved.AssuranceEvents.Count);
    }

    [Fact]
    public async Task Cod_settlement_requires_delivery_and_records_actual_amount_once()
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await CreateService(scope.ServiceProvider).CreateOrderAsync(users[0], Checkout(productId), "assurance");
        var assurance = new OrderAssuranceService(db, Mock.Of<IUserNotificationService>());
        Assert.False(await assurance.RecordCodSettlementAsync(order.Id, "admin", 1, "COD-1"));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
        Assert.True(await CreateService(scope.ServiceProvider).UpdateStatusAsync(order.Id, OrderStatuses.Delivered));
        Assert.False(await assurance.RecordCodSettlementAsync(order.Id, "admin", order.TotalAmount + 1, "COD-1"));
        Assert.False(await assurance.RecordCodSettlementAsync(order.Id, "admin", order.TotalAmount, " "));
        Assert.True(await assurance.RecordCodSettlementAsync(order.Id, "admin", order.TotalAmount - 1000, "COD-1"));
        Assert.False(await assurance.RecordCodSettlementAsync(order.Id, "admin", order.TotalAmount, "COD-2"));
        db.ChangeTracker.Clear();
        var saved = await db.Orders.Include(o => o.AssuranceEvents).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(order.TotalAmount - 1000, saved.CodSettlementAmount);
        Assert.NotNull(saved.CodSettledAt);
        Assert.Single(saved.AssuranceEvents);
    }

    [Fact]
    public async Task Customer_refund_report_does_not_count_as_customer_receipt()
    {
        var (productId, users) = await SeedCartsAsync(2, 1);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = CreateService(scope.ServiceProvider);
        var order = await service.CreateOrderAsync(users[0], Checkout(productId), "assurance");
        var assurance = new OrderAssuranceService(db, Mock.Of<IUserNotificationService>());
        Assert.False(await assurance.SetRefundReceiptAsync(order.Id, users[0], true, null));
        Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Confirmed));
        Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Shipping));
        Assert.True(await service.UpdateStatusAsync(order.Id, OrderStatuses.Delivered));
        Assert.True(await service.MarkManualRefundCompletedAsync(order.Id, ManualRefundMethods.Cash, "REF-1", "Hoàn theo thỏa thuận"));
        Assert.False(await assurance.SetRefundReceiptAsync(order.Id, "other-user", true, null));
        Assert.True(await assurance.SetRefundReceiptAsync(order.Id, users[0], false, "Chưa thấy khoản hoàn trong tài khoản"));
        Assert.False(await assurance.SetRefundReceiptAsync(order.Id, users[0], true, null));
        Assert.True(await assurance.ResolveRefundIssueAsync(order.Id, "admin", "Đã gửi biên nhận, vui lòng kiểm tra lại."));
        Assert.True(await assurance.SetRefundReceiptAsync(order.Id, users[0], true, null));
        db.ChangeTracker.Clear();
        var saved = await db.Orders.Include(o => o.AssuranceEvents).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(RefundStatuses.Refunded, saved.RefundStatus);
        Assert.Equal(ReceiptStatuses.Confirmed, saved.RefundReceiptStatus);
        Assert.NotNull(saved.RefundReceiptConfirmedAt);
        Assert.Equal(4, saved.AssuranceEvents.Count);
    }
}
