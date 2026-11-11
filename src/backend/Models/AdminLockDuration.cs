using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models;

public enum AdminLockDuration
{
    [Display(Name = "24 giờ")] OneDay = 1,
    [Display(Name = "3 ngày")] ThreeDays = 3,
    [Display(Name = "1 tuần (7 ngày)")] OneWeek = 7,
    [Display(Name = "2 tuần (14 ngày)")] TwoWeeks = 14,
    [Display(Name = "1 tháng (30 ngày)")] OneMonth = 30,
    [Display(Name = "3 tháng (90 ngày)")] ThreeMonths = 90,
    [Display(Name = "Không thời hạn", Order = 20000)] Indefinite = 0
}
