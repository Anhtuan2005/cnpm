using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Accessory_bundle_requires_its_phone_and_checkout_persists_discount_stock_and_counts()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(services);
        var phones = new Category { Name = "Phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var accessories = new Category { Name = "Accessories", Slug = PhoneCatalogScope.AccessoryRootSlug + "-" + Guid.NewGuid().ToString("N") };
        var phone = new Product { Name = "Bundle phone", Description = "Fixture", Category = phones, Price = 1_000_000, Stock = 5, DiscountPercent = 10 };
        var otherPhone = new Product { Name = "Another phone", Description = "Fixture", Category = phones, Price = 2_000_000, Stock = 5 };
        var caseProduct = new Product { Name = "Case for bundle phone", Description = "Compatible with bundle phone", Category = accessories, Price = 99_000, Stock = 5 };
        var wrongCase = new Product { Name = "Case for another phone", Description = "Other model", Category = accessories, Price = 99_000, Stock = 5 };
        var charger = new Product { Name = "Compatible USB-C charger", Description = "Fixture", Category = accessories,
            Price = 249_000, DiscountPercent = 10, Stock = 5, Images = [new ProductImage { ImageUrl = "/images/accessories/charger-20w.webp" }] };
        var unavailable = new Product { Name = "Sold out cable", Description = "Fixture", Category = accessories, Price = 99_000 };
        db.Products.AddRange(phone, otherPhone, caseProduct, wrongCase, charger, unavailable);
        db.CrossSellOffers.AddRange(
            new CrossSellOffer { AnchorProduct = phone, AddOnProduct = caseProduct, DiscountPercent = 20 },
            new CrossSellOffer { AnchorProduct = phone, AddOnProduct = otherPhone, DiscountPercent = 90 },
            new CrossSellOffer { AnchorProduct = phone, AddOnProduct = charger, DiscountPercent = 15, EndDate = DateTime.UtcNow.AddDays(-1) },
            new CrossSellOffer { AnchorProduct = phone, AddOnProduct = unavailable, DiscountPercent = 10 },
            new CrossSellOffer { AnchorProduct = otherPhone, AddOnProduct = wrongCase, DiscountPercent = 50 });
        await db.SaveChangesAsync();
        var features = new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 3 }));
        var catalog = new PhoneCatalogScope(features);
        var crossSell = new CrossSellService(db, catalog);
        Assert.Empty(await crossSell.GetSavedPhoneSuggestionsAsync(user.Id));
        db.WishlistItems.AddRange(
            new WishlistItem { UserId = user.Id, ProductId = otherPhone.Id, AddedAt = DateTime.UtcNow.AddDays(-1) },
            new WishlistItem { UserId = user.Id, ProductId = phone.Id },
            new WishlistItem { UserId = user.Id, ProductId = caseProduct.Id, AddedAt = DateTime.UtcNow.AddMinutes(1) });
        await db.SaveChangesAsync();
        var savedSuggestions = await crossSell.GetSavedPhoneSuggestionsAsync(user.Id);
        Assert.Equal(new[] { caseProduct.Id, charger.Id }, savedSuggestions.Select(suggestion => suggestion.ProductId));
        Assert.All(savedSuggestions, suggestion => Assert.Equal(phone.Name, suggestion.PhoneName));
        Assert.Equal(99_000m, savedSuggestions[0].RetailPrice);

        var productService = new ProductService(db, catalog, features);
        var adminCatalog = await productService.GetPagedProductsAsync(null, null, null, null, "newest", 1, 100, includeAccessories: true);
        Assert.Contains(adminCatalog.Products, product => product.Id == caseProduct.Id);
        var accessoryCatalog = await productService.GetPagedProductsAsync(null, accessories.Id, null, null, "newest", 1, 100);
        Assert.True(accessoryCatalog.IsAccessoryCatalog);
        var defaultCatalog = await productService.GetPagedProductsAsync(null, null, null, null, "newest", 1, 100);
        Assert.DoesNotContain(defaultCatalog.Products, product => product.Id == caseProduct.Id);
        var cart = new CartService(db, crossSell, features, catalog);
        await cart.AddAsync(phone.Id, 1, user.Id, "accessory-test");
        var suggestion = Assert.Single((await cart.GetCartAsync(user.Id, "accessory-test")).CrossSellSuggestions);
        Assert.Equal(caseProduct.Id, suggestion.ProductId);
        Assert.Equal(79_200m, suggestion.OfferPrice);
        await cart.AddAsync(caseProduct.Id, 1, user.Id, "accessory-test");
        Assert.Equal(2, await cart.GetCountAsync(user.Id, "accessory-test"));
        var selectedAccessoryOnly = await cart.GetCartAsync(user.Id, "accessory-test", [caseProduct.Id]);
        Assert.Equal(99_000m, selectedAccessoryOnly.Total);
        Assert.Equal(0m, selectedAccessoryOnly.CrossSellDiscountAmount);

        var orders = new OrderService(db, cart, new ShippingFeeService(), Mock.Of<IOrderEmailService>(),
            Mock.Of<ICustomerSegmentService>(), crossSell, Mock.Of<IUserNotificationService>(), Hub(),
            NullLogger<OrderService>.Instance, features, catalog);
        var checkout = Checkout(phone.Id);
        checkout.SelectedProductIds.Add(caseProduct.Id);
        var order = await orders.CreateOrderAsync(user.Id, checkout, "accessory-test");
        Assert.Equal(999_200m, order.TotalAmount);
        Assert.Equal(79_200m, order.Items.Single(item => item.ProductId == caseProduct.Id).UnitPrice);
        db.ChangeTracker.Clear();
        Assert.Equal(4, (await db.Products.FindAsync(caseProduct.Id))!.Stock);
        Assert.Equal(-1, (await db.StockLogs.SingleAsync(row => row.OrderId == order.Id && row.ProductId == caseProduct.Id)).ChangeAmount);
        Assert.False(await db.CartItems.AnyAsync(item => item.Cart!.UserId == user.Id));
        Assert.False(new PhoneCatalogScope(new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 2 })))
            .IsSupportedCategory(accessories.Slug));

        Assert.Empty(await crossSell.GetAfterPurchaseSuggestionsAsync(order));
        order.Status = OrderStatuses.Delivered;
        await db.Products.Where(product => product.Id == phone.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, 0));
        var afterPurchase = Assert.Single(await crossSell.GetAfterPurchaseSuggestionsAsync(order));
        Assert.Equal(charger.Id, afterPurchase.ProductId);
        Assert.Equal(phone.Name, afterPurchase.PhoneName);
        Assert.Equal(224_100m, afterPurchase.RetailPrice);
        Assert.Equal("/images/accessories/charger-20w.webp", afterPurchase.ImageUrl);
        order.RefundStatus = RefundStatuses.Refunded;
        Assert.Empty(await crossSell.GetAfterPurchaseSuggestionsAsync(order));
    }

    [Fact]
    public async Task Accessory_catalog_seed_replaces_phone_bundles_and_preserves_administrator_overrides()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new Category { Name = "Seed phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var phone = new Product { Name = "iPhone 15 Pro", Description = "Fixture", Category = category, Price = 1_000_000, Stock = 5 };
        var secondPhone = new Product { Name = "Legacy bundle phone", Description = "Fixture", Category = category, Price = 500_000, Stock = 5 };
        var oldOffer = new CrossSellOffer { AnchorProduct = phone, AddOnProduct = secondPhone, DiscountPercent = 5 };
        db.CrossSellOffers.Add(oldOffer);
        await db.SaveChangesAsync();
        await PhoneAccessoryCatalog.EnsureAsync(db);
        Assert.False((await db.CrossSellOffers.FindAsync(oldOffer.Id))!.IsActive);
        var caseProduct = await db.Products.Include(product => product.Images)
            .SingleAsync(product => product.Name == "Ốp lưng trong suốt iPhone 15 Pro");
        var caseImage = Assert.Single(caseProduct.Images);
        Assert.Equal("/images/accessories/case-iphone-15-pro.webp", caseImage.ImageUrl);
        caseImage.ImageUrl = "/images/accessories/case.svg";
        caseProduct.Images.Add(new ProductImage { ImageUrl = "/uploads/custom-case.jpg", SortOrder = 1 });
        var offer = await db.CrossSellOffers.SingleAsync(row => row.AnchorProductId == phone.Id && row.AddOnProductId == caseProduct.Id);
        using var http = new HttpClient();
        var chat = new GeminiChatService(http, db, new ConfigurationBuilder().Build(), NullLogger<GeminiChatService>.Instance, new PhoneCatalogScope());
        Assert.Equal(caseProduct.Id, Assert.Single((await chat.AskAsync("Tôi cần ốp lưng iPhone 15 Pro", [])).Products).Id);
        Assert.Empty((await chat.AskAsync("Tôi cần ốp lưng iPhone 14", [])).Products);
        var beforeCount = await db.CrossSellOffers.CountAsync();
        caseProduct.Stock = 2; caseProduct.Price = 123_000; offer.IsActive = false;
        db.Products.Add(new Product { Name = caseProduct.Name, Description = "Separate SKU with the same display name",
            Price = 200_000, Stock = 3, CategoryId = caseProduct.CategoryId });
        await db.SaveChangesAsync();
        await PhoneAccessoryCatalog.EnsureAsync(db);
        Assert.Equal(beforeCount, await db.CrossSellOffers.CountAsync());
        Assert.Equal(123_000m, caseProduct.Price);
        Assert.Equal(2, caseProduct.Stock);
        Assert.False(offer.IsActive);
        Assert.Equal("/images/accessories/case-iphone-15-pro.webp", caseImage.ImageUrl);
        Assert.Contains(caseProduct.Images, image => image.ImageUrl == "/uploads/custom-case.jpg");

        var customer = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var newPhone = new Product { Name = "Phone added after catalog seeding", Description = "Fixture",
            Category = category, Price = 2_000_000, Stock = 5 };
        db.WishlistItems.Add(new WishlistItem { UserId = customer.Id, Product = newPhone });
        await db.SaveChangesAsync();
        Assert.False(await db.CrossSellOffers.AnyAsync(row => row.AnchorProductId == newPhone.Id));

        var crossSell = new CrossSellService(db, new PhoneCatalogScope());
        var fallback = Assert.Single(await crossSell.GetSavedPhoneSuggestionsAsync(customer.Id));
        Assert.Equal(PhoneAccessoryCatalog.UniversalEarbudsName, fallback.ProductName);
        Assert.Equal(newPhone.Name, fallback.PhoneName);
        Assert.Equal(499_000m, fallback.RetailPrice);
        Assert.False(await db.CrossSellOffers.AnyAsync(row => row.AnchorProductId == newPhone.Id));
    }
}
