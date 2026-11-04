using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

// A direct checkout is a quote, never a persisted shopping cart.
public class BuyNowService(AppDbContext db, ICrossSellService crossSell,
    ISprintFeatureService? sprintFeatures = null, ICatalogScope? catalogScope = null)
{
    public async Task<CartViewModel?> BuildAsync(int productId, int quantity, IEnumerable<int>? accessoryIds = null)
    {
        var ids = (accessoryIds ?? []).Distinct().ToArray();
        var promotionsEnabled = sprintFeatures?.IsEnabled(3) ?? true;
        if (productId <= 0 || quantity <= 0 || ids.Length > 12 || ids.Any(id => id <= 0 || id == productId)
            || (!promotionsEnabled && ids.Length > 0)) return null;

        var products = db.Products.Include(product => product.Category).Include(product => product.Images).AsQueryable();
        if (catalogScope is not null) products = catalogScope.Products(products);
        var requestedIds = ids.Append(productId).ToArray();
        var found = await products.Where(product => requestedIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id);
        if (found.Count != requestedIds.Length || found[productId].Stock < quantity
            || ids.Any(id => found[id].Stock < 1)) return null;

        var items = new List<CartItem> { new() { ProductId = productId, Product = found[productId], Quantity = quantity } };
        items.AddRange(ids.Select(id => new CartItem { ProductId = id, Product = found[id], Quantity = 1 }));
        var prices = promotionsEnabled
            ? await crossSell.GetEligibleUnitPricesAsync(items)
            : new Dictionary<int, decimal>();
        // Every selected add-on must have a current offer for this phone. Never trust
        // the URL to grant discounts or introduce another phone / an incompatible case.
        if (ids.Any(id => !prices.ContainsKey(id))) return null;

        return new CartViewModel { Items = items, PromotionsEnabled = promotionsEnabled, CrossSellUnitPrices = prices };
    }
}
