using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;
using System.Security.Claims;

namespace EcommerceApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly IRecommendationService _recommendationService;
    private readonly ICrossSellService _crossSellService;
    private readonly ISprintFeatureService _sprintFeatures;
    private readonly AppDbContext _db;
    private readonly ICatalogScope? _catalogScope;

    public HomeController(ILogger<HomeController> logger, IProductService productService, IRecommendationService recommendationService,
        ICrossSellService crossSellService, ISprintFeatureService sprintFeatures, AppDbContext db, ICatalogScope? catalogScope = null)
    {
        _logger = logger;
        _productService = productService;
        _recommendationService = recommendationService;
        _crossSellService = crossSellService;
        _sprintFeatures = sprintFeatures;
        _db = db;
        _catalogScope = catalogScope;
    }

    public async Task<IActionResult> Index()
    {
        var banners = await _db.Banners
            .Where(banner => banner.IsActive)
            .OrderBy(banner => banner.SortOrder)
            .ToListAsync();
        if (_catalogScope is not null)
            banners = banners.Where(_catalogScope.IsPhoneBanner).ToList();

        var productNameQuery = _db.Products.AsNoTracking().AsQueryable();
        if (_catalogScope is not null)
            productNameQuery = _catalogScope.Products(productNameQuery);
        productNameQuery = PhoneCatalogScope.OnlyPhones(productNameQuery);
        var productNames = await productNameQuery.Select(product => product.Name).ToListAsync();

        var flashSaleQuery = _db.Products
            .Include(product => product.Category)
            .Include(product => product.Images)
            .AsQueryable();
        if (_catalogScope is not null)
            flashSaleQuery = _catalogScope.Products(flashSaleQuery);
        flashSaleQuery = PhoneCatalogScope.OnlyPhones(flashSaleQuery);

        var model = new HomeViewModel
        {
            Banners = banners,
            Brands = PhoneBrandCatalog.Summarize(productNames),
            FeaturedProducts = await _productService.GetFeaturedProductsAsync(8),
            Recommendations = _sprintFeatures.IsEnabled(3) ? await GetHomeRecommendationsAsync() : new([], []),
            FlashSaleProducts = _sprintFeatures.IsEnabled(3)
                ? await flashSaleQuery
                    .Where(product => product.DiscountPercent > 0 && product.Stock > 0)
                    .OrderByDescending(product => product.DiscountPercent)
                    .ThenBy(product => product.Price)
                    .Take(6)
                    .ToListAsync()
                : Array.Empty<Product>()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    [MinimumSprint(3)]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Recommendations() =>
        PartialView("_HomeRecommendations", await GetHomeRecommendationsAsync());

    private async Task<HomeRecommendationsViewModel> GetHomeRecommendationsAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cancellationToken = HttpContext.RequestAborted;
        var phones = await _recommendationService.GetRecommendationsAsync(
            userId, HttpContext.Session.Id, cancellationToken: cancellationToken);
        var accessories = userId is not null && User.IsInRole("User") && !User.IsInRole("Admin")
            ? await _crossSellService.GetSavedPhoneSuggestionsAsync(userId, cancellationToken)
            : [];
        return new HomeRecommendationsViewModel(phones, accessories);
    }

    [MinimumSprint(2)]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status([FromQuery] int code)
    {
        if (code is < 400 or > 599)
        {
            code = StatusCodes.Status500InternalServerError;
        }

        Response.StatusCode = code;
        ViewData["StatusCode"] = code;
        if (code == StatusCodes.Status429TooManyRequests)
        {
            var message = int.TryParse(Response.Headers.RetryAfter.ToString(), out var seconds) && seconds > 0
                ? $"Bạn thao tác quá nhanh. Vui lòng thử lại sau {seconds} giây."
                : "Bạn thao tác quá nhanh. Vui lòng thử lại sau.";
            var originalRequest = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            if (string.Equals(originalRequest?.OriginalPath.TrimEnd('/'), "/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                var query = QueryHelpers.ParseQuery(originalRequest?.OriginalQueryString);
                if (query.TryGetValue("returnUrl", out var returnUrl) && Url.IsLocalUrl(returnUrl.ToString()))
                    ViewData["ReturnUrl"] = returnUrl.ToString();
                ModelState.AddModelError(string.Empty, message);
                return View("~/Views/Account/Login.cshtml", new LoginViewModel());
            }

            ViewData["StatusTitle"] = "Bạn thao tác quá nhanh";
            ViewData["StatusMessage"] = message;
            return View("Status");
        }

        if (code >= 500)
        {
            ViewData["StatusTitle"] = "Oops! Đã xảy ra lỗi.";
            ViewData["StatusMessage"] = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.";
        }
        else
        {
            ViewData["StatusTitle"] = code == 404 ? "Không tìm thấy trang" : "Có lỗi xảy ra";
            ViewData["StatusMessage"] = code == 404
                ? "Trang bạn đang mở có thể đã được di chuyển hoặc không còn tồn tại."
                : "Techvora chưa thể xử lý yêu cầu này. Bạn quay lại trang chủ hoặc thử lại sau một chút.";
        }
        return View("Status");
    }
}
