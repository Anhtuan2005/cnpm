using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public class OrderAssuranceEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    [Required, StringLength(60)]
    public string Type { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Detail { get; set; }
    [StringLength(256)]
    public string? ActorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class OrderAssuranceEventTypes
{
    public const string DeliveryConfirmed = "Khách xác nhận nhận hàng";
    public const string DeliveryIssue = "Khách báo sự cố giao hàng";
    public const string DeliveryIssueHandled = "Admin phản hồi sự cố giao hàng";
    public const string CodSettled = "Admin ghi nhận đối soát COD";
    public const string RefundSent = "Admin ghi nhận đã chuyển tiền hoàn";
    public const string RefundConfirmed = "Khách xác nhận nhận tiền hoàn";
    public const string RefundIssue = "Khách báo chưa nhận tiền hoàn";
    public const string RefundIssueHandled = "Admin phản hồi sự cố hoàn tiền";
}
