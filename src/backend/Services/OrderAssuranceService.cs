using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Data;

namespace EcommerceApp.Services;

public sealed class OrderAssuranceService(AppDbContext db, IUserNotificationService notifications) : IOrderAssuranceService
{
    public async Task<bool> SetDeliveryReceiptAsync(int orderId, string userId, bool received, string? reason)
    {
        var detail = reason?.Trim();
        if (!received && (string.IsNullOrWhiteSpace(detail) || detail.Length > 500)) return false;
        var order = await DatabaseTransaction.ExecuteAsync<Order?>(db, async () =>
        {
            var current = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (current is null || current.UserId != userId || current.Status != OrderStatuses.Delivered ||
                current.CustomerReceiptStatus != ReceiptStatuses.Pending) return null;
            var now = DateTime.UtcNow;
            current.CustomerReceiptStatus = received ? ReceiptStatuses.Confirmed : ReceiptStatuses.Issue;
            if (received) current.CustomerReceiptConfirmedAt = now;
            else
            {
                current.CustomerReceiptIssue = detail;
                current.CustomerReceiptIssueAt = now;
                current.CustomerReceiptResolution = null;
            }
            current.UpdatedAt = now;
            AddEvent(current, received ? OrderAssuranceEventTypes.DeliveryConfirmed : OrderAssuranceEventTypes.DeliveryIssue,
                detail, userId, now);
            await db.SaveChangesAsync();
            return current;
        }, IsolationLevel.Serializable);
        if (order is null) return false;
        if (!received)
            await notifications.CreateForAdminsAsync("Khách báo sự cố giao hàng",
                $"Đơn #DH{order.Id:D4}: khách báo chưa nhận hàng hoặc có vấn đề. Vui lòng kiểm tra chứng từ giao hàng.",
                NotificationTypes.Order, $"/Admin/Order/{order.Id}");
        return true;
    }

    public async Task<bool> ResolveDeliveryIssueAsync(int orderId, string actorId, string note)
    {
        var detail = note?.Trim();
        if (string.IsNullOrWhiteSpace(detail) || detail.Length > 500) return false;
        var order = await DatabaseTransaction.ExecuteAsync<Order?>(db, async () =>
        {
            var current = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (current is null || current.Status != OrderStatuses.Delivered ||
                current.CustomerReceiptStatus != ReceiptStatuses.Issue) return null;
            current.CustomerReceiptStatus = ReceiptStatuses.Pending;
            current.CustomerReceiptResolution = detail;
            current.UpdatedAt = DateTime.UtcNow;
            AddEvent(current, OrderAssuranceEventTypes.DeliveryIssueHandled, detail, actorId, current.UpdatedAt);
            await db.SaveChangesAsync();
            return current;
        }, IsolationLevel.Serializable);
        if (order is null) return false;
        await notifications.CreateAsync(order.UserId, "Shop đã phản hồi sự cố giao hàng",
            $"Đơn #DH{order.Id:D4}: vui lòng xem phản hồi và xác nhận lại tình trạng nhận hàng.",
            NotificationTypes.Order, $"/Order/Detail/{order.Id}");
        return true;
    }

    public Task<bool> RecordDeliveryProofAsync(int orderId, string actorId, string url)
    {
        var value = url?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 500 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return Task.FromResult(false);
        return DatabaseTransaction.ExecuteAsync(db, async () =>
        {
            var order = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (order is null || order.Status != OrderStatuses.Delivered) return false;
            order.DeliveryProofUrl = value;
            order.UpdatedAt = DateTime.UtcNow;
            AddEvent(order, "Admin lưu chứng từ giao hàng", value, actorId, order.UpdatedAt);
            await db.SaveChangesAsync();
            return true;
        }, IsolationLevel.Serializable);
    }

    public Task<bool> RecordCodSettlementAsync(int orderId, string actorId, decimal amount, string reference)
    {
        var value = reference?.Trim();
        if (amount <= 0 || amount != decimal.Truncate(amount) || string.IsNullOrWhiteSpace(value) || value.Length > 120) return Task.FromResult(false);
        return DatabaseTransaction.ExecuteAsync(db, async () =>
        {
            var order = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (order is null || order.Status != OrderStatuses.Delivered || !order.IsPaid ||
                !string.Equals(order.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase) ||
                order.CodSettledAt.HasValue || amount > order.TotalAmount) return false;
            order.CodSettledAt = DateTime.UtcNow;
            order.CodSettlementAmount = amount;
            order.CodSettlementReference = value;
            order.CodSettlementConfirmedBy = actorId;
            order.UpdatedAt = order.CodSettledAt.Value;
            AddEvent(order, OrderAssuranceEventTypes.CodSettled,
                $"{amount:N0} ₫ · {value}", actorId, order.UpdatedAt);
            await db.SaveChangesAsync();
            return true;
        }, IsolationLevel.Serializable);
    }

    public async Task<bool> SetRefundReceiptAsync(int orderId, string userId, bool received, string? reason)
    {
        var detail = reason?.Trim();
        if (!received && (string.IsNullOrWhiteSpace(detail) || detail.Length > 500)) return false;
        var order = await DatabaseTransaction.ExecuteAsync<Order?>(db, async () =>
        {
            var current = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (current is null || current.UserId != userId || current.RefundStatus != RefundStatuses.Refunded ||
                current.RefundReceiptStatus != ReceiptStatuses.Pending) return null;
            var now = DateTime.UtcNow;
            current.RefundReceiptStatus = received ? ReceiptStatuses.Confirmed : ReceiptStatuses.Issue;
            if (received) current.RefundReceiptConfirmedAt = now;
            else
            {
                current.RefundReceiptIssue = detail;
                current.RefundReceiptIssueAt = now;
                current.RefundReceiptResolution = null;
            }
            current.UpdatedAt = now;
            AddEvent(current, received ? OrderAssuranceEventTypes.RefundConfirmed : OrderAssuranceEventTypes.RefundIssue,
                detail, userId, now);
            await db.SaveChangesAsync();
            return current;
        }, IsolationLevel.Serializable);
        if (order is null) return false;
        if (!received)
            await notifications.CreateForAdminsAsync("Khách báo chưa nhận tiền hoàn",
                $"Đơn #DH{order.Id:D4}: vui lòng kiểm tra mã giao dịch và đối soát khoản hoàn.",
                NotificationTypes.Order, $"/Admin/Order/{order.Id}");
        return true;
    }

    public async Task<bool> ResolveRefundIssueAsync(int orderId, string actorId, string note)
    {
        var detail = note?.Trim();
        if (string.IsNullOrWhiteSpace(detail) || detail.Length > 500) return false;
        var order = await DatabaseTransaction.ExecuteAsync<Order?>(db, async () =>
        {
            var current = await OrderLifecycle.LoadForUpdateAsync(db, orderId);
            if (current is null || current.RefundStatus != RefundStatuses.Refunded ||
                current.RefundReceiptStatus != ReceiptStatuses.Issue) return null;
            current.RefundReceiptStatus = ReceiptStatuses.Pending;
            current.RefundReceiptResolution = detail;
            current.UpdatedAt = DateTime.UtcNow;
            AddEvent(current, OrderAssuranceEventTypes.RefundIssueHandled, detail, actorId, current.UpdatedAt);
            await db.SaveChangesAsync();
            return current;
        }, IsolationLevel.Serializable);
        if (order is null) return false;
        await notifications.CreateAsync(order.UserId, "Shop đã phản hồi khoản hoàn tiền",
            $"Đơn #DH{order.Id:D4}: vui lòng kiểm tra phản hồi và xác nhận tình trạng nhận tiền.",
            NotificationTypes.Order, $"/Order/Detail/{order.Id}");
        return true;
    }

    private static void AddEvent(Order order, string type, string? detail, string actorId, DateTime now) =>
        order.AssuranceEvents.Add(new OrderAssuranceEvent
        {
            Type = type, Detail = detail, ActorId = actorId, CreatedAt = now
        });
}
