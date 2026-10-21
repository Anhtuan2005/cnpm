using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using EcommerceApp.Services;

namespace EcommerceApp.Tests;

public class ProductComparisonAdvisorTests
{
    [Fact]
    public void Phones_from_different_brand_categories_are_comparable()
    {
        var iphone = Product(1, "iPhone 15 Pro", "dien-thoai-apple", 1, 28_990_000);
        var galaxy = Product(2, "Samsung Galaxy S24", "dien-thoai-samsung", 2, 21_990_000);
        var laptop = Product(3, "Laptop", "laptop", 3, 20_000_000);

        Assert.True(ProductComparisonAdvisor.CanCompare(iphone, galaxy));
        Assert.False(ProductComparisonAdvisor.CanCompare(iphone, laptop));
    }

    [Fact]
    public void Recommendation_identifies_price_and_measurable_advantages_across_brands()
    {
        var iphone = Product(1, "iPhone 15 Pro", "dien-thoai-apple", 1, 28_990_000);
        var xiaomi = Product(2, "Xiaomi 14", "dien-thoai-xiaomi", 2, 14_990_000);
        var specs = new ProductSpecService();
        var items = new[] { Item(iphone, specs), Item(xiaomi, specs) };

        var verdict = ProductComparisonAdvisor.Build(items, useSalePrice: false);

        Assert.Equal(xiaomi.Id, verdict.WinnerProductId);
        Assert.Contains("Xiaomi 14", verdict.Headline);
        Assert.Contains(verdict.Highlights, advantage => advantage.ProductId == xiaomi.Id && advantage.Text.Contains("Giá thấp hơn"));
        Assert.Contains(verdict.Highlights, advantage => advantage.ProductId == xiaomi.Id && advantage.Text.Contains("RAM") && advantage.Text.Contains("4 GB"));
    }

    [Fact]
    public void Ambiguous_spec_ranges_do_not_create_a_winner()
    {
        var first = Product(1, "Máy A", "dien-thoai-a", 1, 10_000_000);
        var second = Product(2, "Máy B", "dien-thoai-b", 2, 10_000_000);
        var items = new[]
        {
            new ProductCompareItemViewModel { Product = first, Specs = new Dictionary<string, string> { ["RAM / Bộ nhớ"] = "8GB-12GB RAM, 128GB-256GB lưu trữ", ["Pin và sạc"] = "Pin dùng cả ngày" } },
            new ProductCompareItemViewModel { Product = second, Specs = new Dictionary<string, string> { ["RAM / Bộ nhớ"] = "8GB RAM, 256GB lưu trữ", ["Pin và sạc"] = "Sạc nhanh" } }
        };

        var verdict = ProductComparisonAdvisor.Build(items, useSalePrice: false);

        Assert.Null(verdict.WinnerProductId);
        Assert.Empty(verdict.Highlights);
    }

    [Fact]
    public void Available_phone_is_recommended_when_the_other_is_out_of_stock()
    {
        var available = Product(1, "Máy còn hàng", "dien-thoai-a", 1, 12_000_000);
        var unavailable = Product(2, "Máy hết hàng", "dien-thoai-b", 2, 10_000_000);
        unavailable.Stock = 0;

        var verdict = ProductComparisonAdvisor.Build(new[]
        {
            new ProductCompareItemViewModel { Product = available },
            new ProductCompareItemViewModel { Product = unavailable }
        }, useSalePrice: false);

        Assert.Equal(available.Id, verdict.WinnerProductId);
        Assert.Contains(verdict.Highlights, advantage => advantage.ProductId == available.Id && advantage.Text.Contains("còn hàng"));
        Assert.Contains(verdict.Highlights, advantage => advantage.ProductId == unavailable.Id && advantage.Text.Contains("Giá thấp hơn"));
    }

    [Fact]
    public void Price_only_recommendation_is_conditional()
    {
        var first = Product(1, "Máy A", "dien-thoai-a", 1, 12_000_000);
        var second = Product(2, "Máy B", "dien-thoai-b", 2, 10_000_000);

        var verdict = ProductComparisonAdvisor.Build(new[]
        {
            new ProductCompareItemViewModel { Product = first },
            new ProductCompareItemViewModel { Product = second }
        }, useSalePrice: false);

        Assert.Equal(second.Id, verdict.WinnerProductId);
        Assert.Contains("Nếu ưu tiên giá", verdict.Headline);
        Assert.Contains("chưa phản ánh", verdict.Detail);
    }

    private static Product Product(int id, string name, string slug, int categoryId, decimal price) =>
        new() { Id = id, Name = name, CategoryId = categoryId, Category = new Category { Id = categoryId, Name = "Điện thoại", Slug = slug }, Price = price, Stock = 1 };

    private static ProductCompareItemViewModel Item(Product product, ProductSpecService specs) => new()
    {
        Product = product,
        Specs = specs.Build(product).ToDictionary(spec => spec.Label, spec => spec.Value)
    };
}
