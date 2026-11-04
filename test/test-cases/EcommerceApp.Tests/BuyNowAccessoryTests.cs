using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Buy_now_quotes_and_orders_discounted_accessories_without_modifying_the_saved_cart()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(services);
        var phone = new Product { Name = "Direct phone", Description = "Fixture", Price = 1_000_000, Stock = 5,
            DiscountPercent = 10, Category = new Category { Name = "Phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") } };
        var accessory = new Product { Name = "Direct case", Description = "Fixture", Price = 99_000, Stock = 5,
            Category = new Category { Name = "Accessories", Slug = PhoneCatalogScope.AccessoryRootSlug + "-" + Guid.NewGuid().ToString("N") } };
        db.CrossSellOffers.Add(new CrossSellOffer { AnchorProduct = phone, AddOnProduct = accessory, DiscountPercent = 20 });
        db.Carts.Add(new Cart { UserId = user.Id, Items = [new CartItem { Product = phone, Quantity = 1 }, new CartItem { Product = accessory, Quantity = 3 }] });
        await db.SaveChangesAsync();
        var catalog = new PhoneCatalogScope();
        var crossSell = new CrossSellService(db, catalog);
        var buyNow = new BuyNowService(db, crossSell, catalogScope: catalog);
        var quote = await buyNow.BuildAsync(phone.Id, 2, [accessory.Id, accessory.Id]);
        Assert.NotNull(quote);
        Assert.Equal(2, quote.Items.Count());
        Assert.Equal(1_879_200m, quote.Total);
        Assert.Equal(19_800m, quote.CrossSellDiscountAmount);

        var model = Checkout(phone.Id);
        model.BuyNowProductId = phone.Id; model.BuyNowQuantity = 2;
        model.BuyNowAccessoryIds = [accessory.Id, accessory.Id];
        model.SelectedProductIds.Clear();
        // Client-supplied totals must never determine the order price.
        model.DiscountAmount = 999_999;
        var orders = new OrderService(db, new CartService(db, crossSell), new ShippingFeeService(), Mock.Of<IOrderEmailService>(),
            Mock.Of<ICustomerSegmentService>(), crossSell, Mock.Of<IUserNotificationService>(), Hub(), NullLogger<OrderService>.Instance,
            catalogScope: catalog);
        var order = await orders.CreateOrderAsync(user.Id, model, "direct-test");
        Assert.Equal(1_899_200m, order.TotalAmount);
        Assert.Equal(79_200m, order.Items.Single(item => item.ProductId == accessory.Id).UnitPrice);
        Assert.Equal(1, order.Items.Single(item => item.ProductId == accessory.Id).Quantity);
        db.ChangeTracker.Clear();
        Assert.Equal(3, (await db.Products.FindAsync(phone.Id))!.Stock);
        Assert.Equal(4, (await db.Products.FindAsync(accessory.Id))!.Stock);
        Assert.Equal(1, await db.CartItems.Where(item => item.Cart!.UserId == user.Id && item.ProductId == phone.Id).Select(item => item.Quantity).SingleAsync());
        Assert.Equal(3, await db.CartItems.Where(item => item.Cart!.UserId == user.Id && item.ProductId == accessory.Id).Select(item => item.Quantity).SingleAsync());
        Assert.Equal(2, await db.StockLogs.CountAsync(log => log.OrderId == order.Id));
    }

    [Theory]
    [InlineData("wrong-phone")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("disabled")]
    [InlineData("out-of-stock")]
    [InlineData("deleted")]
    [InlineData("second-phone")]
    [InlineData("sprint-two")]
    public async Task Buy_now_rejects_unavailable_or_ineligible_addons_before_creating_an_order(string condition)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(services);
        var phones = new Category { Name = "Phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        var phone = new Product { Name = "Direct validation phone", Description = "Fixture", Price = 1_000_000, Stock = 5, Category = phones };
        var otherPhone = new Product { Name = "Different phone", Description = "Fixture", Price = 2_000_000, Stock = 5, Category = phones };
        var accessory = new Product { Name = "Direct validation case", Description = "Fixture", Price = 99_000, Stock = condition == "out-of-stock" ? 0 : 5,
            IsDeleted = condition == "deleted", Category = new Category { Name = "Accessories", Slug = PhoneCatalogScope.AccessoryRootSlug + "-" + Guid.NewGuid().ToString("N") } };
        var selected = condition == "second-phone" ? otherPhone : accessory;
        var offer = new CrossSellOffer { AnchorProduct = condition == "wrong-phone" ? otherPhone : phone,
            AddOnProduct = selected, DiscountPercent = 20, IsActive = condition != "disabled",
            StartDate = condition == "future" ? DateTime.UtcNow.AddDays(1) : null,
            EndDate = condition == "expired" ? DateTime.UtcNow.AddDays(-1) : null };
        db.Products.AddRange(phone, otherPhone, accessory);
        db.CrossSellOffers.Add(offer);
        await db.SaveChangesAsync();
        var features = new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = condition == "sprint-two" ? 2 : 3 }));
        var catalog = new PhoneCatalogScope(features);
        var crossSell = new CrossSellService(db, catalog);
        var buyNow = new BuyNowService(db, crossSell, features, catalog);
        var model = Checkout(phone.Id);
        model.BuyNowProductId = phone.Id; model.BuyNowQuantity = 1;
        model.BuyNowAccessoryIds = [selected.Id]; model.SelectedProductIds.Clear();
        Assert.Null(await buyNow.BuildAsync(phone.Id, 1, model.BuyNowAccessoryIds));
        var orders = new OrderService(db, new CartService(db, crossSell, features, catalog), new ShippingFeeService(), Mock.Of<IOrderEmailService>(),
            Mock.Of<ICustomerSegmentService>(), crossSell, Mock.Of<IUserNotificationService>(), Hub(), NullLogger<OrderService>.Instance, features, catalog);
        await Assert.ThrowsAsync<InvalidOperationException>(() => orders.CreateOrderAsync(user.Id, model, "direct-test"));
        Assert.False(await db.Orders.AnyAsync(order => order.UserId == user.Id));
        Assert.False(await db.Carts.AnyAsync(cart => cart.UserId == user.Id));
        Assert.Equal(5, (await db.Products.AsNoTracking().SingleAsync(product => product.Id == phone.Id)).Stock);
    }
}
