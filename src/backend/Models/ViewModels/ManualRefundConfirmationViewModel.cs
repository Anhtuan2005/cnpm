using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models.ViewModels;

public sealed class ManualRefundConfirmationViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn kênh đã hoàn tiền.")]
    public string Method { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mã giao dịch hoàn tiền hoặc mã biên nhận.")]
    [StringLength(120, ErrorMessage = "Mã giao dịch tối đa 120 ký tự.")]
    public string Reference { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "Ghi chú hoàn tiền tối đa 500 ký tự.")]
    public string? Note { get; set; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "Vui lòng xác nhận đã hoàn đủ tiền cho khách hàng.")]
    public bool Confirmed { get; set; }
}
