using System.Security.Claims;
using EcommerceApp.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

public sealed class WishlistViewState(AppDbContext db, ICatalogScope catalogScope, IHttpContextAccessor httpContextAccessor)
{
    private Task<IReadOnlySet<int>>? _productIds;

    // All product cards and the navigation badge share the same query for this request.
    public Task<IReadOnlySet<int>> GetProductIdsAsync() => _productIds ??= LoadProductIdsAsync();

    private async Task<IReadOnlySet<int>> LoadProductIdsAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.User.Identity?.IsAuthenticated != true || context.User.IsInRole("Admin"))
            return new HashSet<int>();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return new HashSet<int>();

        var catalogProductIds = catalogScope.Products(db.Products).Select(product => product.Id);
        var productIds = await db.WishlistItems
            .Where(item => item.UserId == userId && catalogProductIds.Contains(item.ProductId))
            .Select(item => item.ProductId)
            .ToListAsync(context.RequestAborted);
        return productIds.ToHashSet();
    }
}
