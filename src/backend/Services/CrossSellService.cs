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

    public Task<IReadOnlyList<AccessorySuggestion>> GetAfterPurchaseSuggestionsAsync(
        Order order, CancellationToken cancellationToken = default)
    {
        if (order.Status != OrderStatuses.Delivered
            || order.RefundStatus is RefundStatuses.PendingManual or RefundStatuses.Refunded)
            return Task.FromResult<IReadOnlyList<AccessorySuggestion>>([]);

        var purchasedIds = order.Items.Select(item => item.ProductId).ToHashSet();
        return GetRetailSuggestionsAsync(purchasedIds, purchasedIds, 8, cancellationToken);
    }

    public async Task<IReadOnlyList<AccessorySuggestion>> GetSavedPhoneSuggestionsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var catalog = _catalogScope?.Products(_db.Products) ?? _db.Products;
        var phoneIds = PhoneCatalogScope.OnlyPhones(catalog).Select(product => product.Id);
        var savedPhone = await _db.WishlistItems
            .Where(item => item.UserId == userId && phoneIds.Contains(item.ProductId))
            .OrderByDescending(item => item.AddedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new { item.ProductId, item.Product!.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (savedPhone is null) return [];

        var compatible = await GetRetailSuggestionsAsync([savedPhone.ProductId], [], 6, cancellationToken);
        if (compatible.Count > 0) return compatible;

        var earbuds = await PhoneCatalogScope.OnlyAccessories(catalog).AsNoTracking()
            .Include(product => product.Images)
            .Where(product => product.Name == PhoneAccessoryCatalog.UniversalEarbudsName && product.Stock > 0)
            .OrderBy(product => product.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return earbuds is null
            ? []
            : [new AccessorySuggestion(earbuds.Id, earbuds.Name, savedPhone.Name, earbuds.PrimaryImageUrl, earbuds.SalePrice)];
    }

    private async Task<IReadOnlyList<AccessorySuggestion>> GetRetailSuggestionsAsync(
        IReadOnlyCollection<int> phoneIds, IReadOnlyCollection<int> excludedIds, int take, CancellationToken cancellationToken)
    {
        var offers = await CompatibleOffers().AsNoTracking()
            .Include(offer => offer.AnchorProduct)
            .Include(offer => offer.AddOnProduct).ThenInclude(product => product!.Images)
            .Where(offer => phoneIds.Contains(offer.AnchorProductId) && !excludedIds.Contains(offer.AddOnProductId))
            .OrderBy(offer => offer.Id)
            .ToListAsync(cancellationToken);

        return offers.DistinctBy(offer => offer.AddOnProductId)
            .Take(take)
            .Select(offer => new AccessorySuggestion(
                offer.AddOnProductId, offer.AddOnProduct!.Name, offer.AnchorProduct!.Name,
                offer.AddOnProduct.PrimaryImageUrl, offer.AddOnProduct.SalePrice))
            .ToList();
    }

    private IQueryable<CrossSellOffer> CompatibleOffers()
    {
        var catalog = _catalogScope?.Products(_db.Products) ?? _db.Products;
        var phones = PhoneCatalogScope.OnlyPhones(catalog).Select(product => product.Id);
        var accessories = PhoneCatalogScope.OnlyAccessories(catalog).Where(product => product.Stock > 0).Select(product => product.Id);
        return _db.CrossSellOffers.Where(offer => offer.IsActive
            && phones.Contains(offer.AnchorProductId) && accessories.Contains(offer.AddOnProductId));
    }

    private IQueryable<CrossSellOffer> ActiveOffers()
    {
        var now = DateTime.UtcNow;
        return CompatibleOffers().Where(offer => offer.AnchorProduct!.Stock > 0
            && offer.DiscountPercent >= 1 && offer.DiscountPercent <= 90
            && (offer.StartDate == null || offer.StartDate <= now)
            && (offer.EndDate == null || offer.EndDate >= now));
    }

    private static decimal DiscountedPrice(decimal price, int discountPercent) =>
        Math.Round(price * (100 - discountPercent) / 100m, 0, MidpointRounding.AwayFromZero);
}
