using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EcommerceApp.Tests;

public sealed partial class OrderPaymentTests
{
    [Fact]
    public async Task Flash_sale_only_contains_available_discounted_phones_before_counting_and_paging()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new Category { Name = "Sale phones", Slug = "dien-thoai-" + Guid.NewGuid().ToString("N") };
        Product Phone(string name, int discount, int stock = 3) => new()
        {
            Name = name, Description = "Flash sale fixture", Category = category,
            Price = 1_000_000, Stock = stock, DiscountPercent = discount
        };
        var biggestDeal = Phone("Sale phone one", 20);
        var secondDeal = Phone("Sale phone two", 10);
        var regular = Phone("Regular phone", 0);
        var soldOut = Phone("Sold out deal", 80, 0);
        var deleted = Phone("Deleted deal", 90); deleted.IsDeleted = true;
        var accessory = Phone("Accessory deal", 50);
        accessory.Category = new Category { Name = "Accessories", Slug = "accessories-" + Guid.NewGuid().ToString("N") };
        db.Products.AddRange(biggestDeal, secondDeal, regular, soldOut, deleted, accessory);
        await db.SaveChangesAsync();
        var features = new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 3 }));
        var service = new ProductService(db, new PhoneCatalogScope(), features);
        var firstPage = await service.GetPagedProductsAsync(null, category.Id, null, null, "discount_desc", 1, 1);
        Assert.Equal(2, firstPage.TotalItems);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(biggestDeal.Id, Assert.Single(firstPage.Products).Id);
        var secondPage = await service.GetPagedProductsAsync(null, category.Id, null, null, "discount_desc", 2, 1);
        Assert.Equal(secondDeal.Id, Assert.Single(secondPage.Products).Id);
        Assert.True(firstPage.SaleOnly);
        Assert.Empty((await service.GetPagedProductsAsync(null, accessory.CategoryId, null, null, "discount_desc", 1, 12)).Products);
        var priceSort = await service.GetPagedProductsAsync(null, category.Id, 850_000, 950_000, "price_asc", 1, 12, saleOnly: true);
        Assert.Equal(secondDeal.Id, Assert.Single(priceSort.Products).Id);
        Assert.True(priceSort.SaleOnly);
        var allPhones = await service.GetPagedProductsAsync(null, category.Id, null, null, "featured", 1, 12);
        Assert.False(allPhones.SaleOnly);
        Assert.Contains(allPhones.Products, product => product.Id == regular.Id);
        var sprintTwo = new ProductService(db, new PhoneCatalogScope(),
            new SprintFeatureService(Options.Create(new SprintFeatureOptions { ActiveSprint = 2 })));
        var rollback = await sprintTwo.GetPagedProductsAsync(null, category.Id, null, null, "discount_desc", 1, 12, saleOnly: true);
        Assert.False(rollback.SaleOnly);
        Assert.Contains(rollback.Products, product => product.Id == regular.Id);
    }
}
