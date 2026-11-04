using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

public class CrossSellService : ICrossSellService
{
    private readonly AppDbContext _db;
    private readonly ICatalogScope? _catalogScope;

    public CrossSellService(AppDbContext db, ICatalogScope? catalogScope = null)
    {
        _db = db;
        _catalogScope = catalogScope;
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetEligibleUnitPricesAsync(IEnumerable<CartItem> cartItems, CancellationToken cancellationToken = default)
    {
        var items = cartItems.Where(item => item.Product is not null && item.Quantity > 0).ToList();
        var ids = items.Select(item => item.ProductId).ToHashSet();
        var offers = await ActiveOffers()
            .Where(offer => ids.Contains(offer.AnchorProductId) && ids.Contains(offer.AddOnProductId))
            .ToListAsync(cancellationToken);
        return offers.GroupBy(offer => offer.AddOnProductId).ToDictionary(group => group.Key, group =>
            DiscountedPrice(items.First(item => item.ProductId == group.Key).Product!.SalePrice,
                group.Max(offer => offer.DiscountPercent)));
    }

    public async Task<IReadOnlyList<CrossSellSuggestionViewModel>> GetSuggestionsAsync(IEnumerable<CartItem> cartItems, int take = 4, CancellationToken cancellationToken = default)
    {
        var ids = cartItems.Where(item => item.Product is not null && item.Quantity > 0).Select(item => item.ProductId).ToHashSet();
        if (ids.Count == 0 || take <= 0) return Array.Empty<CrossSellSuggestionViewModel>();

        var offers = await ActiveOffers().AsNoTracking()
            .Include(offer => offer.AnchorProduct)
            .Include(offer => offer.AddOnProduct).ThenInclude(product => product!.Images)
            .Include(offer => offer.AddOnProduct).ThenInclude(product => product!.Category)
            .Where(offer => ids.Contains(offer.AnchorProductId) && !ids.Contains(offer.AddOnProductId))
            .ToListAsync(cancellationToken);

        // A configured offer defines compatibility. Purchase history must not recommend
        // another phone, a case for another model, or an accessory without a bundle discount.
        return offers.GroupBy(offer => offer.AddOnProductId)
            .Select(group => group.OrderByDescending(offer => offer.DiscountPercent).ThenBy(offer => offer.Id).First())
            .OrderByDescending(offer => offer.DiscountPercent)
            .ThenBy(offer => offer.AddOnProduct!.SalePrice)
            .ThenBy(offer => offer.AddOnProductId)
            .Take(Math.Min(take, 12))
            .Select(offer => new CrossSellSuggestionViewModel
            {
                OfferId = offer.Id, ProductId = offer.AddOnProductId,
                ProductName = offer.AddOnProduct!.Name, AnchorProductName = offer.AnchorProduct!.Name,
                ImageUrl = offer.AddOnProduct.PrimaryImageUrl, Source = "manual",
                DiscountPercent = offer.DiscountPercent, OriginalPrice = offer.AddOnProduct.SalePrice,
                OfferPrice = DiscountedPrice(offer.AddOnProduct.SalePrice, offer.DiscountPercent)
            }).ToList();
    }

    private IQueryable<CrossSellOffer> ActiveOffers()
    {
        var now = DateTime.UtcNow;
        var catalog = _catalogScope?.Products(_db.Products) ?? _db.Products;
        var phones = PhoneCatalogScope.OnlyPhones(catalog).Where(product => product.Stock > 0).Select(product => product.Id);
        var accessories = PhoneCatalogScope.OnlyAccessories(catalog).Where(product => product.Stock > 0).Select(product => product.Id);
        return _db.CrossSellOffers.Where(offer => offer.IsActive
            && offer.DiscountPercent >= 1 && offer.DiscountPercent <= 90
            && phones.Contains(offer.AnchorProductId) && accessories.Contains(offer.AddOnProductId)
            && (offer.StartDate == null || offer.StartDate <= now)
            && (offer.EndDate == null || offer.EndDate >= now));
    }

    private static decimal DiscountedPrice(decimal price, int discountPercent) =>
        Math.Round(price * (100 - discountPercent) / 100m, 0, MidpointRounding.AwayFromZero);
}
