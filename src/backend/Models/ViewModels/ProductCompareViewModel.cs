namespace EcommerceApp.Models.ViewModels;

public class ProductCompareViewModel
{
    public IReadOnlyList<ProductCompareItemViewModel> Products { get; set; } = Array.Empty<ProductCompareItemViewModel>();
    public IReadOnlyList<string> SpecLabels { get; set; } = Array.Empty<string>();
    public bool HasRejectedProducts { get; set; }
    public IReadOnlyList<ProductComparisonNeedOption> Needs { get; set; } = Array.Empty<ProductComparisonNeedOption>();
    public string SelectedNeed { get; set; } = "balanced";
    public ProductComparisonVerdictViewModel Verdict { get; set; } = new();
}

public record ProductComparisonNeedOption(string Id, string Label, string Description);

public record ProductComparisonBadgeViewModel(string Tone, string Symbol, string Text);

public record ProductComparisonAdvantageViewModel(int ProductId, string Text, int Weight = 1);

public record ProductComparisonVerdictViewModel(
    int? WinnerProductId = null,
    string Headline = "",
    string Detail = "",
    IReadOnlyList<ProductComparisonAdvantageViewModel>? Advantages = null,
    string Method = "")
{
    public IReadOnlyList<ProductComparisonAdvantageViewModel> Highlights => Advantages ?? Array.Empty<ProductComparisonAdvantageViewModel>();
}

public class ProductCompareItemViewModel
{
    public Product Product { get; set; } = new();
    public IReadOnlyDictionary<string, string> Specs { get; set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, IReadOnlyList<ProductComparisonBadgeViewModel>> ComparisonBadges { get; set; } =
        new Dictionary<string, IReadOnlyList<ProductComparisonBadgeViewModel>>(StringComparer.OrdinalIgnoreCase);
}
