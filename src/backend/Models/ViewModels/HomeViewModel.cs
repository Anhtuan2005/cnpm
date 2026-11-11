namespace EcommerceApp.Models.ViewModels;

public class HomeViewModel
{
    public IEnumerable<Banner> Banners { get; set; } = Enumerable.Empty<Banner>();
    public IEnumerable<PhoneBrandViewModel> Brands { get; set; } = Enumerable.Empty<PhoneBrandViewModel>();
    public IEnumerable<Product> FeaturedProducts { get; set; } = Enumerable.Empty<Product>();
    public HomeRecommendationsViewModel Recommendations { get; set; } = new([], []);
    public IEnumerable<Product> FlashSaleProducts { get; set; } = Enumerable.Empty<Product>();
}

public sealed record PhoneBrandViewModel(string Name, string SearchTerm, string Mark, int ProductCount);

public sealed record HomeRecommendationsViewModel(IReadOnlyList<Product> Phones, IReadOnlyList<AccessorySuggestion> Accessories)
{
    public bool HasResults => Phones.Count > 0 || Accessories.Count > 0;
}
