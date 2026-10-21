namespace EcommerceApp.Services;

public interface IOrderAssuranceService
{
    Task<bool> SetDeliveryReceiptAsync(int orderId, string userId, bool received, string? reason);
    Task<bool> ResolveDeliveryIssueAsync(int orderId, string actorId, string note);
    Task<bool> RecordDeliveryProofAsync(int orderId, string actorId, string url);
    Task<bool> RecordCodSettlementAsync(int orderId, string actorId, decimal amount, string reference);
    Task<bool> SetRefundReceiptAsync(int orderId, string userId, bool received, string? reason);
    Task<bool> ResolveRefundIssueAsync(int orderId, string actorId, string note);
}
