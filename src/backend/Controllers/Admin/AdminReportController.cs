using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Services;
using System.Text;

namespace EcommerceApp.Controllers.Admin;

[EcommerceApp.Services.MinimumSprint(3)]
[Authorize(Roles = "Admin")]
[Route("Admin/Report")]
public class AdminReportController : Controller
{
    private readonly AppDbContext _db;

    public AdminReportController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string period = "30", DateTime? fromDate = null, DateTime? toDate = null)
    {
        return View("~/Views/Admin/Report/Index.cshtml", await BuildReportAsync(period, fromDate, toDate));
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(string period = "30", DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await BuildReportAsync(period, fromDate, toDate);
        var csv = new StringBuilder();
        csv.AppendLine(CsvFormatter.Row("TuNgayUTC", "DenNgayUTC", "TienHang", "GiamGiaVoucher", "PhiGiaoHang", "DoanhThu", "SoDon"));
        csv.AppendLine(CsvFormatter.Row(report.FromDate, report.ToDate, report.MerchandiseRevenue,
            report.VoucherDiscount, report.ShippingRevenue, report.CurrentPeriodRevenue, report.TotalOrders));
        csv.AppendLine(CsvFormatter.Row("NgayUTC", "DoanhThu"));
        foreach (var day in report.DailyRevenue)
            csv.AppendLine(CsvFormatter.Row(day.Label, day.Revenue));
        csv.AppendLine(CsvFormatter.Row("DanhMuc", "TienHangTruocVoucher"));
        foreach (var category in report.RevenueByCategory.OrderByDescending(row => row.Value))
            csv.AppendLine(CsvFormatter.Row(category.Key, category.Value));
        csv.AppendLine(CsvFormatter.Row("TopSanPham", "SoLuong", "TienHangTruocVoucher"));
        foreach (var product in report.TopProducts)
            csv.AppendLine(CsvFormatter.Row(product.ProductName, product.QuantitySold, product.Revenue));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.csv");
    }

    private async Task<AdminReportViewModel> BuildReportAsync(string period, DateTime? fromDate, DateTime? toDate)
    {
        period = period is "7" or "30" or "90" or "365" or "custom" ? period : "30";
        var today = DateTime.UtcNow.Date;
        var selectedEnd = (toDate ?? today).Date;
        if (selectedEnd > today)
        {
            selectedEnd = today;
            ViewData["ReportFilterError"] = "Ngày kết thúc trong tương lai đã được đưa về hôm nay.";
        }
        else if (selectedEnd < DateTime.UnixEpoch.Date)
        {
            selectedEnd = DateTime.UnixEpoch.Date;
            ViewData["ReportFilterError"] = "Ngày kết thúc quá cũ đã được đưa về 01/01/1970.";
        }

        var end = selectedEnd.AddDays(1);
        var start = period switch
        {
            "7" => end.AddDays(-7),
            "90" => end.AddMonths(-3),
            "365" => end.AddYears(-1),
            "custom" => (fromDate ?? selectedEnd.AddDays(-29)).Date,
            _ => end.AddDays(-30)
        };

        if (period == "custom" && start > selectedEnd)
        {
            start = selectedEnd.AddDays(-29);
            ViewData["ReportFilterError"] = "Từ ngày phải trước hoặc bằng đến ngày. Báo cáo đang hiển thị 30 ngày gần nhất.";
        }

        var earliestAllowed = selectedEnd.AddDays(-365);
        if (start < earliestAllowed)
        {
            start = earliestAllowed;
            ViewData["ReportFilterError"] = "Khoảng tùy chọn được giới hạn tối đa 366 ngày.";
        }

        var previousStart = start.AddDays(-(end - start).TotalDays);

        var revenueOrders = _db.Orders.WithRecognizedRevenue();
        var currentOrders = revenueOrders.Where(order => order.CreatedAt >= start && order.CreatedAt < end);
        var previousOrders = revenueOrders.Where(order => order.CreatedAt >= previousStart && order.CreatedAt < start);
        var currentRevenue = await currentOrders.SumAsync(order => order.TotalAmount);
        var previousRevenue = await previousOrders.SumAsync(order => order.TotalAmount);
        var dailyRevenue = await currentOrders
            .GroupBy(order => order.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Revenue = group.Sum(order => order.TotalAmount) })
            .OrderBy(point => point.Date)
            .ToListAsync();
        var revenueByDate = dailyRevenue.ToDictionary(point => point.Date, point => point.Revenue);
        var dailyPoints = Enumerable.Range(0, Math.Max(1, (int)(end.Date - start.Date).TotalDays))
            .Select(offset =>
            {
                var date = start.Date.AddDays(offset);
                return new RevenuePointViewModel
                {
                    Label = date.ToString("yyyy-MM-dd"),
                    Revenue = revenueByDate.GetValueOrDefault(date)
                };
            })
            .ToList();

        var model = new AdminReportViewModel
        {
            Period = period,
            FromDate = start.Date,
            ToDate = end.AddDays(-1).Date,
            CurrentPeriodRevenue = currentRevenue,
            PreviousPeriodRevenue = previousRevenue,
            GrowthPercent = previousRevenue == 0 ? null : (currentRevenue - previousRevenue) / previousRevenue * 100,
            MerchandiseRevenue = await _db.OrderItems.IgnoreQueryFilters().WithRecognizedRevenue()
                .Where(item => item.Order!.CreatedAt >= start && item.Order.CreatedAt < end)
                .SumAsync(item => item.Quantity * item.UnitPrice),
            VoucherDiscount = await currentOrders.SumAsync(order => order.DiscountAmount),
            ShippingRevenue = await currentOrders.SumAsync(order => order.ShippingFee),
            TotalOrders = await currentOrders.CountAsync(),
            TotalCustomers = await _db.Users.CountAsync(),
            DailyRevenue = dailyPoints,
            RevenueByCategory = await _db.OrderItems
                .IgnoreQueryFilters()
                .Include(item => item.Product).ThenInclude(product => product!.Category)
                .WithRecognizedRevenue()
                .Where(item => item.Order!.CreatedAt >= start && item.Order.CreatedAt < end)
                .GroupBy(item => item.Product!.Category!.Name)
                .ToDictionaryAsync(group => group.Key, group => group.Sum(item => item.Quantity * item.UnitPrice)),
            TopProducts = await _db.OrderItems
                .IgnoreQueryFilters()
                .Include(item => item.Product)
                .WithRecognizedRevenue()
                .Where(item => item.Order!.CreatedAt >= start && item.Order.CreatedAt < end)
                .GroupBy(item => new { item.ProductId, item.Product!.Name })
                .Select(group => new TopProductViewModel { ProductName = group.Key.Name, QuantitySold = group.Sum(item => item.Quantity), Revenue = group.Sum(item => item.Quantity * item.UnitPrice) })
                .OrderByDescending(item => item.QuantitySold)
                .ThenBy(item => item.ProductName)
                .Take(10)
                .ToListAsync()
        };

        return model;
    }
}
