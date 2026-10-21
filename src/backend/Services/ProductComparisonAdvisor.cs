using System.Globalization;
using System.Text.RegularExpressions;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;

namespace EcommerceApp.Services;

public static class ProductComparisonAdvisor
{
    private const string BalancedNeed = "balanced";
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly IReadOnlyList<NeedProfile> Profiles = new[]
    {
        new NeedProfile(
            new ProductComparisonNeedOption(BalancedNeed, "Cân bằng", "Cân bằng giữa giá, hiệu năng, pin và bộ nhớ."),
            Weights(("price", 1), ("ram", 1), ("storage", 1), ("battery", 1), ("charging", 1), ("refresh", 1), ("chip", 1), ("camera-main", 1), ("camera-ois", 1), ("camera-telephoto", 1), ("camera-ultrawide", 1), ("camera-selfie", 1))),
        new NeedProfile(
            new ProductComparisonNeedOption("student", "Sinh viên, ưu tiên tiết kiệm", "Ưu tiên giá dễ chịu, bộ nhớ đủ dùng và pin cho cả ngày."),
            Weights(("price", 6), ("ram", 2), ("storage", 3), ("battery", 2), ("charging", 1), ("refresh", 1), ("chip", 1))),
        new NeedProfile(
            new ProductComparisonNeedOption("gaming", "Chơi game", "Ưu tiên chip, RAM, màn hình mượt và bộ nhớ."),
            Weights(("price", 1), ("ram", 4), ("storage", 2), ("battery", 1), ("charging", 1), ("refresh", 4), ("chip", 6))),
        new NeedProfile(
            new ProductComparisonNeedOption("camera", "Chụp ảnh, quay video", "Ưu tiên thông số camera, chống rung và các ống kính."),
            Weights(("price", 1), ("storage", 1), ("chip", 1), ("camera-main", 3), ("camera-ois", 3), ("camera-telephoto", 2), ("camera-ultrawide", 1), ("camera-selfie", 2))),
        new NeedProfile(
            new ProductComparisonNeedOption("battery", "Pin lâu", "Ưu tiên dung lượng pin, sau đó là tốc độ sạc và giá."),
            Weights(("price", 2), ("ram", 1), ("storage", 1), ("battery", 6), ("charging", 2))),
        new NeedProfile(
            new ProductComparisonNeedOption("work", "Học tập, làm việc", "Ưu tiên đa nhiệm, bộ nhớ, pin và hiệu năng ổn định."),
            Weights(("price", 2), ("ram", 3), ("storage", 3), ("battery", 2), ("charging", 1), ("refresh", 1), ("chip", 2)))
    };

    private static readonly IReadOnlyList<NeedProfile> GeneralProfiles = new[]
    {
        new NeedProfile(
            new ProductComparisonNeedOption(BalancedNeed, "Cân bằng", "Cân bằng giữa giá, hiệu năng và bộ nhớ."),
            Weights(("price", 1), ("ram", 1), ("storage", 1), ("refresh", 1), ("chip", 1))),
        new NeedProfile(
            new ProductComparisonNeedOption("student", "Sinh viên, ưu tiên tiết kiệm", "Ưu tiên giá dễ chịu, bộ nhớ đủ dùng và pin cho cả ngày."),
            Weights(("price", 6), ("ram", 2), ("storage", 3), ("battery", 2), ("charging", 1), ("chip", 1))),
        new NeedProfile(
            new ProductComparisonNeedOption("gaming", "Chơi game", "Ưu tiên hiệu năng, bộ nhớ và màn hình mượt."),
            Weights(("price", 1), ("ram", 4), ("storage", 2), ("refresh", 4), ("chip", 6))),
        new NeedProfile(
            new ProductComparisonNeedOption("work", "Học tập, làm việc", "Ưu tiên đa nhiệm, bộ nhớ và hiệu năng ổn định."),
            Weights(("price", 2), ("ram", 3), ("storage", 3), ("battery", 2), ("charging", 1), ("chip", 2)))
    };

    private static readonly IReadOnlyDictionary<string, double> KnownChipTiers = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["Snapdragon 8 Gen 3"] = 3,
        ["Snapdragon 8 Gen 2"] = 3,
        ["Apple A17 Pro"] = 3,
        ["Apple A16 Bionic"] = 3,
        ["Exynos 2400"] = 3,
        ["Google Tensor G3"] = 2,
        ["Snapdragon 7 Gen 3"] = 2,
        ["Snapdragon 6 Gen 1"] = 1,
        ["Dimensity 7050"] = 1
    };

    public static bool CanCompare(Product first, Product second) =>
        first.CategoryId == second.CategoryId ||
        (PhoneCatalogScope.IsPhoneSlug(first.Category?.Slug) && PhoneCatalogScope.IsPhoneSlug(second.Category?.Slug));

    public static IReadOnlyList<ProductComparisonNeedOption> GetAvailableNeeds(IReadOnlyList<ProductCompareItemViewModel> items)
    {
        if (items.Count != 2) return new[] { Profiles[0].Option };
        var isPhoneComparison = items.Count == 2 && items.All(item => PhoneCatalogScope.IsPhoneSlug(item.Product.Category?.Slug));
        return isPhoneComparison
            ? Profiles.Select(profile => profile.Option).ToList()
            : GeneralProfiles.Select(profile => profile.Option).ToList();
    }

    public static string NormalizeNeed(string? need, IReadOnlyList<ProductComparisonNeedOption> availableNeeds)
    {
        var match = availableNeeds.FirstOrDefault(option =>
            string.Equals(option.Id, need?.Trim(), StringComparison.OrdinalIgnoreCase));
        return match?.Id ?? BalancedNeed;
    }

    public static ProductComparisonVerdictViewModel Build(
        IReadOnlyList<ProductCompareItemViewModel> items,
        bool useSalePrice,
        string? selectedNeed = null)
    {
        if (items.Count != 2) return new();

        var first = items[0];
        var second = items[1];
        var isPhoneComparison = items.All(item => PhoneCatalogScope.IsPhoneSlug(item.Product.Category?.Slug));
        var availableProfiles = isPhoneComparison ? Profiles : GeneralProfiles;
        var profile = availableProfiles.FirstOrDefault(item =>
            string.Equals(item.Option.Id, selectedNeed, StringComparison.OrdinalIgnoreCase)) ?? availableProfiles[0];
        var method = BuildMethod(profile);
        var criteria = new List<ComparisonCriterion>();

        var firstPrice = useSalePrice ? first.Product.SalePrice : first.Product.Price;
        var secondPrice = useSalePrice ? second.Product.SalePrice : second.Product.Price;
        AddLowerCriterion("price", firstPrice, secondPrice, first, second,
            difference => $"Giá thấp hơn {difference.ToString("N0", VietnameseCulture)} ₫", criteria);

        AddHigherCriterion("ram", ReadRam(first), ReadRam(second), first, second,
            difference => $"RAM nhiều hơn {difference.ToString("N0", VietnameseCulture)} GB", criteria);
        AddHigherCriterion("storage", ReadStorage(first), ReadStorage(second), first, second,
            difference => $"Bộ nhớ lớn hơn {difference.ToString("N0", VietnameseCulture)} GB", criteria);
        var firstBattery = ReadBattery(first);
        var secondBattery = ReadBattery(second);
        var batteryUnit = ReadBatteryUnit(first);
        if (!string.Equals(batteryUnit, ReadBatteryUnit(second), StringComparison.OrdinalIgnoreCase))
        {
            firstBattery = null;
            secondBattery = null;
            batteryUnit = string.Empty;
        }
        AddHigherCriterion("battery", firstBattery, secondBattery, first, second,
            difference => $"Pin có dung lượng/thời lượng cao hơn {difference.ToString("N0", VietnameseCulture)} {batteryUnit}".TrimEnd(), criteria);
        AddHigherCriterion("charging", ReadNumber(first, new[] { "Pin và sạc", "Pin" }, @"\b(?<number>\d{2,3})\s*W\b"),
            ReadNumber(second, new[] { "Pin và sạc", "Pin" }, @"\b(?<number>\d{2,3})\s*W\b"), first, second,
            difference => $"Công suất sạc cao hơn {difference.ToString("N0", VietnameseCulture)} W", criteria);
        AddHigherCriterion("refresh", ReadNumber(first, new[] { "Màn hình", "Tần số quét" }, @"\b(?<number>\d{2,3})\s*Hz\b"),
            ReadNumber(second, new[] { "Màn hình", "Tần số quét" }, @"\b(?<number>\d{2,3})\s*Hz\b"), first, second,
            difference => $"Tần số quét cao hơn {difference.ToString("N0", VietnameseCulture)} Hz", criteria);
        AddChipCriterion(first, second, criteria);

        AddHigherCriterion("camera-main", ReadCameraMegapixels(first, selfie: false), ReadCameraMegapixels(second, selfie: false), first, second,
            difference => $"Camera chính có độ phân giải cao hơn {difference.ToString("N0", VietnameseCulture)} MP", criteria);
        AddHigherCriterion("camera-selfie", ReadCameraMegapixels(first, selfie: true), ReadCameraMegapixels(second, selfie: true), first, second,
            difference => $"Camera trước có độ phân giải cao hơn {difference.ToString("N0", VietnameseCulture)} MP", criteria);
        AddFeatureCriterion("camera-ois", HasCameraFeature(first, "OIS"), HasCameraFeature(second, "OIS"), first, second,
            "Có chống rung quang học OIS", criteria);
        AddFeatureCriterion("camera-telephoto", HasCameraFeature(first, "telephoto", "tele"), HasCameraFeature(second, "telephoto", "tele"), first, second,
            "Có ống kính telephoto", criteria);
        AddFeatureCriterion("camera-ultrawide", HasCameraFeature(first, "ultra-wide", "ultrawide"), HasCameraFeature(second, "ultra-wide", "ultrawide"), first, second,
            "Có camera góc siêu rộng", criteria);

        var onlyFirstAvailable = first.Product.Stock > 0 && second.Product.Stock <= 0;
        var onlySecondAvailable = second.Product.Stock > 0 && first.Product.Stock <= 0;
        if (onlyFirstAvailable || onlySecondAvailable)
        {
            var available = onlyFirstAvailable ? first : second;
            var winnerHighlights = criteria
                .OrderByDescending(item => profile.Weights.GetValueOrDefault(item.NeedMetric))
                .Select(item => new ProductComparisonAdvantageViewModel(item.WinnerProductId, item.Text, profile.Weights.GetValueOrDefault(item.NeedMetric)))
                .ToList();
            winnerHighlights.Insert(0, new(available.Product.Id, "Đang còn hàng để đặt mua", 6));
            return new(available.Product.Id, $"Nếu cần mua ngay, chọn {available.Product.Name}",
                $"Sản phẩm còn lại đang hết hàng. Các ưu thế theo nhu cầu {profile.Option.Label.ToLowerInvariant()} được liệt kê bên dưới.",
                winnerHighlights, $"Tình trạng còn hàng được ưu tiên vì quyết định khả năng mua ngay. {method}");
        }

        var weightedCriteria = criteria
            .Where(item => profile.Weights.GetValueOrDefault(item.NeedMetric) > 0)
            .OrderByDescending(item => profile.Weights.GetValueOrDefault(item.NeedMetric))
            .ToList();
        var highlights = weightedCriteria
            .Select(item => new ProductComparisonAdvantageViewModel(item.WinnerProductId, item.Text, profile.Weights[item.NeedMetric]))
            .ToList();

        if (weightedCriteria.Count == 0)
        {
            return new(null, $"Chưa có bên nào nổi trội theo nhu cầu {profile.Option.Label.ToLowerInvariant()}",
                "Các tiêu chí đo được đang ngang nhau, hoặc dữ liệu hiện có chưa đủ để phân định. Hãy xem từng thông số và ưu tiên cá nhân trước khi chọn.", highlights, method);
        }

        var firstScore = weightedCriteria.Where(item => item.WinnerProductId == first.Product.Id)
            .Sum(item => profile.Weights[item.NeedMetric]);
        var secondScore = weightedCriteria.Where(item => item.WinnerProductId == second.Product.Id)
            .Sum(item => profile.Weights[item.NeedMetric]);
        if (firstScore == secondScore)
        {
            return new(null, $"Hai máy cân bằng theo nhu cầu {profile.Option.Label.ToLowerInvariant()}",
                "Mỗi máy có ưu thế ở một số tiêu chí quan trọng tương đương. Chọn theo các thông số cụ thể bên dưới hoặc ưu tiên cá nhân của bạn.", highlights, method);
        }

        var winner = firstScore > secondScore ? first : second;
        if (weightedCriteria.Count == 1 && weightedCriteria[0].NeedMetric == "price")
        {
            var headline = profile.Option.Id == BalancedNeed
                ? $"Nếu ưu tiên giá, chọn {winner.Product.Name}"
                : $"Hợp ưu tiên {profile.Option.Label.ToLowerInvariant()}: {winner.Product.Name}";
            return new(winner.Product.Id, headline,
                "Giá là tiêu chí duy nhất có dữ liệu để so sánh; kết luận này chưa phản ánh các thông số chưa được khai báo.", highlights, method);
        }

        var detail = $"Gợi ý này dựa trên các tiêu chí được ưu tiên cho nhu cầu {profile.Option.Label.ToLowerInvariant()}. Lợi thế của từng máy và điểm đánh đổi được nêu riêng bên dưới.";
        if (profile.Option.Id == "camera")
        {
            detail += " Thông số camera không thay thế được việc xem ảnh và video chụp thực tế.";
        }

        var recommendationHeadline = profile.Option.Id == BalancedNeed
            ? $"Gợi ý chọn {winner.Product.Name}"
            : $"Hợp ưu tiên {profile.Option.Label.ToLowerInvariant()}: {winner.Product.Name}";
        return new(winner.Product.Id, recommendationHeadline, detail, highlights, method);
    }

    private static string BuildMethod(NeedProfile profile)
    {
        var groups = profile.Weights
            .GroupBy(pair => pair.Value)
            .OrderByDescending(group => group.Key)
            .ToList();
        var caveat = "Chỉ tính thông số rõ ràng ở cả hai máy; dữ liệu thiếu hoặc nhiều cấu hình sẽ được bỏ qua.";
        if (groups.Count == 1) return $"Các tiêu chí đo được được tính ngang nhau. {caveat}";

        var weightSummary = groups.Select(group =>
        {
            var metrics = string.Join(", ", group.Select(pair => MetricLabel(pair.Key)));
            return $"{metrics} (trọng số {group.Key})";
        });
        return $"Ưu tiên từ cao xuống thấp: {string.Join("; ", weightSummary)}. {caveat}";
    }

    private static string MetricLabel(string metric) => metric switch
    {
        "price" => "giá",
        "ram" => "RAM",
        "storage" => "bộ nhớ",
        "battery" => "pin",
        "charging" => "công suất sạc",
        "refresh" => "tần số quét",
        "chip" => "chip",
        "camera-main" => "camera chính (MP)",
        "camera-ois" => "chống rung OIS",
        "camera-telephoto" => "ống telephoto",
        "camera-ultrawide" => "camera góc siêu rộng",
        "camera-selfie" => "camera selfie (MP)",
        _ => metric
    };

    public static void ApplyComparisonBadges(IReadOnlyList<ProductCompareItemViewModel> items, bool useSalePrice)
    {
        if (items.Count != 2) return;

        var first = items[0];
        var second = items[1];
        var firstBadges = new Dictionary<string, List<ProductComparisonBadgeViewModel>>(StringComparer.OrdinalIgnoreCase);
        var secondBadges = new Dictionary<string, List<ProductComparisonBadgeViewModel>>(StringComparer.OrdinalIgnoreCase);

        var firstPrice = useSalePrice ? first.Product.SalePrice : first.Product.Price;
        var secondPrice = useSalePrice ? second.Product.SalePrice : second.Product.Price;
        AddNumericBadges("Giá", (double)firstPrice, (double)secondPrice, "Giá", "₫", higherIsBetter: false,
            firstBadges, secondBadges, value => value.ToString("N0", VietnameseCulture),
            higherText: "Đắt hơn", lowerText: "Rẻ hơn");

        AddAvailabilityBadges(first, second, firstBadges, secondBadges);

        var labels = first.Specs.Keys.Concat(second.Specs.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var label in labels)
        {
            if (!first.Specs.TryGetValue(label, out var firstValue) || !second.Specs.TryGetValue(label, out var secondValue)) continue;

            switch (label.ToLowerInvariant())
            {
                case "ram / bộ nhớ":
                    AddNumericBadges(label, ReadRam(first), ReadRam(second), "RAM", "GB", higherIsBetter: true, firstBadges, secondBadges,
                        value => value.ToString("N0", VietnameseCulture), "Nhiều hơn", "Ít hơn");
                    AddNumericBadges(label, ReadStorage(first), ReadStorage(second), "Dung lượng", "GB", higherIsBetter: true, firstBadges, secondBadges,
                        value => value.ToString("N0", VietnameseCulture), "Lớn hơn", "Nhỏ hơn");
                    break;
                case "màn hình":
                case "tần số quét":
                    AddNumericBadges(label,
                        ReadNumber(first, new[] { "Màn hình", "Tần số quét" }, @"\b(?<number>\d{2,3})\s*Hz\b"),
                        ReadNumber(second, new[] { "Màn hình", "Tần số quét" }, @"\b(?<number>\d{2,3})\s*Hz\b"),
                        "Tần số quét", "Hz", higherIsBetter: true, firstBadges, secondBadges,
                        value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn");
                    break;
                case "chip xử lý":
                case "cpu":
                    AddChipBadges(label, first, second, firstBadges, secondBadges);
                    break;
                case "pin và sạc":
                case "pin":
                    var firstBattery = ReadBattery(first);
                    var secondBattery = ReadBattery(second);
                    var batteryUnit = ReadBatteryUnit(first);
                    if (!string.Equals(batteryUnit, ReadBatteryUnit(second), StringComparison.OrdinalIgnoreCase))
                    {
                        firstBattery = null;
                        secondBattery = null;
                        batteryUnit = string.Empty;
                    }
                    AddNumericBadges(label, firstBattery, secondBattery, "Dung lượng pin", batteryUnit, higherIsBetter: true,
                        firstBadges, secondBadges, value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn");
                    AddNumericBadges(label,
                        ReadNumber(first, new[] { "Pin và sạc", "Pin" }, @"\b(?<number>\d{2,3})\s*W\b"),
                        ReadNumber(second, new[] { "Pin và sạc", "Pin" }, @"\b(?<number>\d{2,3})\s*W\b"),
                        "Công suất sạc", "W", higherIsBetter: true, firstBadges, secondBadges,
                        value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn");
                    break;
                case "camera chính":
                    AddNumericBadges(label, ReadMegapixels(first, label), ReadMegapixels(second, label), "Độ phân giải", "MP", higherIsBetter: false,
                        firstBadges, secondBadges, value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn", isMeasurementOnly: true);
                    AddFeatureBadges(label, ReadOis(first), ReadOis(second), "OIS", firstBadges, secondBadges);
                    break;
                case "camera trước":
                case "camera telephoto":
                    AddCameraLensBadges(label, first, second, firstBadges, secondBadges);
                    break;
                case "camera góc siêu rộng":
                    AddCameraLensBadges(label, first, second, firstBadges, secondBadges, isMeasurementOnly: true);
                    break;
                default:
                    if (string.Equals(firstValue.Trim(), secondValue.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        AddBadge(firstBadges, label, new("equal", "=", "Thông số trùng nhau"));
                        AddBadge(secondBadges, label, new("equal", "=", "Thông số trùng nhau"));
                    }
                    break;
            }
        }

        first.ComparisonBadges = FreezeBadges(firstBadges);
        second.ComparisonBadges = FreezeBadges(secondBadges);
    }

    private static void AddAvailabilityBadges(ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> firstBadges,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> secondBadges)
    {
        if ((first.Product.Stock > 0) == (second.Product.Stock > 0))
        {
            var text = first.Product.Stock > 0 ? "Cả hai đều còn hàng" : "Cả hai đều hết hàng";
            AddBadge(firstBadges, "Tình trạng", new("equal", "=", text));
            AddBadge(secondBadges, "Tình trạng", new("equal", "=", text));
            return;
        }

        var availableBadges = first.Product.Stock > 0 ? firstBadges : secondBadges;
        var unavailableBadges = first.Product.Stock > 0 ? secondBadges : firstBadges;
        AddBadge(availableBadges, "Tình trạng", new("advantage", "↑", "Còn hàng"));
        AddBadge(unavailableBadges, "Tình trạng", new("weaker", "↓", "Hết hàng"));
    }

    private static void AddNumericBadges(string key, double? firstValue, double? secondValue, string metric, string unit,
        bool higherIsBetter,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> firstBadges,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> secondBadges,
        Func<double, string> format,
        string higherText,
        string lowerText,
        bool isMeasurementOnly = false)
    {
        if (!firstValue.HasValue || !secondValue.HasValue) return;
        if (Math.Abs(firstValue.Value - secondValue.Value) < 0.0001)
        {
            AddBadge(firstBadges, key, new("equal", "=", $"{metric} ngang nhau"));
            AddBadge(secondBadges, key, new("equal", "=", $"{metric} ngang nhau"));
            return;
        }

        var firstIsHigher = firstValue.Value > secondValue.Value;
        var difference = Math.Abs(firstValue.Value - secondValue.Value);
        var detail = $"{format(difference)} {unit}".Trim();
        var higherTone = isMeasurementOnly ? "stat-higher" : higherIsBetter ? "advantage" : "weaker";
        var lowerTone = isMeasurementOnly ? "stat-lower" : higherIsBetter ? "weaker" : "advantage";
        var higherBadge = new ProductComparisonBadgeViewModel(higherTone, "↑", $"{higherText} {detail}");
        var lowerBadge = new ProductComparisonBadgeViewModel(lowerTone, "↓", $"{lowerText} {detail}");

        AddBadge(firstIsHigher ? firstBadges : secondBadges, key, higherBadge);
        AddBadge(firstIsHigher ? secondBadges : firstBadges, key, lowerBadge);
    }

    private static void AddChipBadges(string key, ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> firstBadges,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> secondBadges)
    {
        var firstChip = FindSpec(first, key, "Chip xử lý", "CPU");
        var secondChip = FindSpec(second, key, "Chip xử lý", "CPU");
        if (string.IsNullOrWhiteSpace(firstChip) || string.IsNullOrWhiteSpace(secondChip)) return;
        if (string.Equals(firstChip.Trim(), secondChip.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            AddBadge(firstBadges, key, new("equal", "=", "Cùng bộ xử lý"));
            AddBadge(secondBadges, key, new("equal", "=", "Cùng bộ xử lý"));
            return;
        }

        var firstTier = ReadChipTier(first);
        var secondTier = ReadChipTier(second);
        if (!firstTier.HasValue || !secondTier.HasValue) return;
        if (firstTier.Value == secondTier.Value)
        {
            AddBadge(firstBadges, key, new("approximate", "≈", "Cùng phân hạng tham khảo"));
            AddBadge(secondBadges, key, new("approximate", "≈", "Cùng phân hạng tham khảo"));
            return;
        }

        var firstIsHigher = firstTier.Value > secondTier.Value;
        AddBadge(firstIsHigher ? firstBadges : secondBadges, key, new("advantage", "↑", "Hiệu năng tham khảo cao hơn"));
        AddBadge(firstIsHigher ? secondBadges : firstBadges, key, new("weaker", "↓", "Phân hạng tham khảo thấp hơn"));
    }

    private static void AddCameraLensBadges(string key, ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> firstBadges,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> secondBadges,
        bool isMeasurementOnly = false)
    {
        var firstValue = FindSpec(first, key);
        var secondValue = FindSpec(second, key);
        if (string.IsNullOrWhiteSpace(firstValue) || string.IsNullOrWhiteSpace(secondValue)) return;

        if (key.Equals("Camera telephoto", StringComparison.OrdinalIgnoreCase))
        {
            AddFeatureBadges(key, ReadFeaturePresence(first, key), ReadFeaturePresence(second, key), "Ống kính telephoto", firstBadges, secondBadges);
            AddNumericBadges(key, ReadMegapixels(first, key), ReadMegapixels(second, key), "Độ phân giải telephoto", "MP", higherIsBetter: false,
                firstBadges, secondBadges, value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn", isMeasurementOnly: true);
            return;
        }

        AddNumericBadges(key, ReadMegapixels(first, key), ReadMegapixels(second, key), "Độ phân giải", "MP", higherIsBetter: false,
            firstBadges, secondBadges, value => value.ToString("N0", VietnameseCulture), "Cao hơn", "Thấp hơn", isMeasurementOnly);
        if (key.Equals("Camera trước", StringComparison.OrdinalIgnoreCase))
        {
            AddFeatureBadges(key, ReadAutofocus(first, key), ReadAutofocus(second, key), "Tự động lấy nét", firstBadges, secondBadges);
        }
    }

    private static void AddFeatureBadges(string key, bool? firstHasFeature, bool? secondHasFeature, string feature,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> firstBadges,
        Dictionary<string, List<ProductComparisonBadgeViewModel>> secondBadges)
    {
        if (!firstHasFeature.HasValue || !secondHasFeature.HasValue) return;
        if (firstHasFeature.Value == secondHasFeature.Value)
        {
            var text = firstHasFeature.Value ? $"Cả hai đều có {feature}" : $"Cả hai đều không có {feature}";
            AddBadge(firstBadges, key, new("equal", "=", text));
            AddBadge(secondBadges, key, new("equal", "=", text));
            return;
        }

        var featureBadges = firstHasFeature.Value ? firstBadges : secondBadges;
        var noFeatureBadges = firstHasFeature.Value ? secondBadges : firstBadges;
        AddBadge(featureBadges, key, new("feature", "+", $"Có {feature}"));
        AddBadge(noFeatureBadges, key, new("feature-muted", "−", $"Không có {feature}"));
    }

    private static double? ReadMegapixels(ProductCompareItemViewModel item, string specLabel)
    {
        var value = FindSpec(item, specLabel);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : ParseNumber(Regex.Match(value, @"(?<number>\d{2,3})\s*MP\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static bool? ReadOis(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "Camera chính", "Camera sau");
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Contains("OIS", StringComparison.OrdinalIgnoreCase)) return true;
        if (Regex.IsMatch(value, @"không\s+(?:có\s+)?OIS", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        return null;
    }

    private static bool? ReadAutofocus(ProductCompareItemViewModel item, string label)
    {
        var value = FindSpec(item, label);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Regex.IsMatch(value, @"\bAF\b|autofocus|tự động lấy nét", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return true;
        if (Regex.IsMatch(value, @"không\s+(?:có\s+)?(?:AF|autofocus|tự động lấy nét)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        return null;
    }

    private static bool? ReadFeaturePresence(ProductCompareItemViewModel item, string label)
    {
        var value = FindSpec(item, label);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Regex.IsMatch(value, @"^\s*không\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        return true;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ProductComparisonBadgeViewModel>> FreezeBadges(
        Dictionary<string, List<ProductComparisonBadgeViewModel>> badges) =>
        badges.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ProductComparisonBadgeViewModel>)pair.Value, StringComparer.OrdinalIgnoreCase);

    private static void AddBadge(Dictionary<string, List<ProductComparisonBadgeViewModel>> badges, string key, ProductComparisonBadgeViewModel badge)
    {
        if (!badges.TryGetValue(key, out var values))
        {
            values = new List<ProductComparisonBadgeViewModel>();
            badges[key] = values;
        }
        values.Add(badge);
    }

    private static IReadOnlyDictionary<string, int> Weights(params (string Key, int Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static void AddLowerCriterion(string metric, decimal firstValue, decimal secondValue,
        ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        Func<decimal, string> text, List<ComparisonCriterion> criteria)
    {
        if (firstValue == secondValue) return;
        var winner = firstValue < secondValue ? first : second;
        criteria.Add(new(metric, winner.Product.Id, text(Math.Abs(firstValue - secondValue))));
    }

    private static void AddHigherCriterion(string metric, double? firstValue, double? secondValue,
        ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        Func<double, string> text, List<ComparisonCriterion> criteria)
    {
        if (!firstValue.HasValue || !secondValue.HasValue || firstValue.Value == secondValue.Value) return;
        var winner = firstValue.Value > secondValue.Value ? first : second;
        criteria.Add(new(metric, winner.Product.Id, text(Math.Abs(firstValue.Value - secondValue.Value))));
    }

    private static void AddFeatureCriterion(string metric, bool firstHasFeature, bool secondHasFeature,
        ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        string label, List<ComparisonCriterion> criteria)
    {
        if (firstHasFeature == secondHasFeature) return;
        var winner = firstHasFeature ? first : second;
        criteria.Add(new(metric, winner.Product.Id, label));
    }

    private static void AddChipCriterion(ProductCompareItemViewModel first, ProductCompareItemViewModel second,
        List<ComparisonCriterion> criteria)
    {
        var firstTier = ReadChipTier(first);
        var secondTier = ReadChipTier(second);
        if (!firstTier.HasValue || !secondTier.HasValue || firstTier.Value == secondTier.Value) return;

        var firstChip = FindSpec(first, "Chip xử lý", "CPU")!;
        var secondChip = FindSpec(second, "Chip xử lý", "CPU")!;
        var firstWins = firstTier.Value > secondTier.Value;
        var winner = firstWins ? first : second;
        var winnerChip = firstWins ? firstChip : secondChip;
        var otherChip = firstWins ? secondChip : firstChip;
        criteria.Add(new("chip", winner.Product.Id,
            $"Chip {winnerChip} được xếp ở phân hạng hiệu năng tham khảo cao hơn {otherChip}"));
    }

    private static double? ReadRam(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "RAM / Bộ nhớ", "RAM");
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Regex.IsMatch(value, @"\d+\s*GB\s*[-–/]\s*\d+\s*GB\s+RAM\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return null;
        var explicitRam = Regex.Match(value, @"(?<number>\d+)\s*GB\s+RAM\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (explicitRam.Success) return ParseNumber(explicitRam);
        if (HasAmbiguousRange(value)) return null;
        return ParseNumber(Regex.Match(value, @"(?<number>\d+)\s*GB\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static double? ReadStorage(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "RAM / Bộ nhớ");
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (Regex.IsMatch(value, @"\d+\s*(?:GB|TB)\s*[-–/]\s*\d+\s*(?:GB|TB)\s+lưu trữ\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return null;
            var storage = Regex.Match(value, @"(?<number>\d+)\s*(?<unit>GB|TB)\s+lưu trữ\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (storage.Success) return ParseCapacity(storage);
        }

        value = FindSpec(item, "Ổ cứng", "Dung lượng");
        if (string.IsNullOrWhiteSpace(value) || HasAmbiguousRange(value)) return null;
        return ParseCapacity(Regex.Match(value, @"(?<number>\d+)\s*(?<unit>GB|TB)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static double? ReadBattery(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "Pin và sạc", "Pin");
        if (string.IsNullOrWhiteSpace(value)) return null;
        var capacity = Regex.Match(value, @"(?<number>\d{3,5})\s*mAh\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (capacity.Success) return ParseNumber(capacity);
        var hours = Regex.Match(value, @"(?<number>\d{1,2})\s*(?:giờ|hours?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return ParseNumber(hours);
    }

    private static string ReadBatteryUnit(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "Pin và sạc", "Pin") ?? string.Empty;
        if (Regex.IsMatch(value, @"\d{3,5}\s*mAh\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return "mAh";
        if (Regex.IsMatch(value, @"\d{1,2}\s*(?:giờ|hours?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return "giờ";
        return string.Empty;
    }

    private static double? ReadNumber(ProductCompareItemViewModel item, IReadOnlyList<string> labels, string pattern)
    {
        var value = FindSpec(item, labels.ToArray());
        return string.IsNullOrWhiteSpace(value)
            ? null
            : ParseNumber(Regex.Match(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static double? ReadChipTier(ProductCompareItemViewModel item)
    {
        var value = FindSpec(item, "Chip xử lý", "CPU");
        if (string.IsNullOrWhiteSpace(value) || value.Contains('/') || value.Contains("tùy", StringComparison.OrdinalIgnoreCase)) return null;
        var match = KnownChipTiers.FirstOrDefault(pair => value.Contains(pair.Key, StringComparison.OrdinalIgnoreCase));
        return match.Key is null ? null : match.Value;
    }

    private static double? ReadCameraMegapixels(ProductCompareItemViewModel item, bool selfie)
    {
        var value = FindSpec(item, "Camera sau", "Camera trước");
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (selfie)
        {
            var selfieMatch = Regex.Match(value, @"selfie\s*(?<number>\d{2,3})\s*MP", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return ParseNumber(selfieMatch);
        }

        var mainMatch = Regex.Match(value, @"(?<number>\d{2,3})\s*MP", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return ParseNumber(mainMatch);
    }

    private static bool HasCameraFeature(ProductCompareItemViewModel item, params string[] features)
    {
        var value = FindSpec(item, "Camera sau", "Camera trước");
        return !string.IsNullOrWhiteSpace(value) && features.Any(feature => value.Contains(feature, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindSpec(ProductCompareItemViewModel item, params string[] labels)
    {
        foreach (var label in labels)
        {
            if (item.Specs.TryGetValue(label, out var value)) return value;
        }
        return null;
    }

    private static bool HasAmbiguousRange(string value) =>
        Regex.IsMatch(value, @"\d\s*(?:-|–|/|hoặc)\s*\d", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static double? ParseCapacity(Match match)
    {
        var number = ParseNumber(match);
        if (!number.HasValue) return null;
        return match.Groups["unit"].Value.Equals("TB", StringComparison.OrdinalIgnoreCase)
            ? number.Value * 1024
            : number.Value;
    }

    private static double? ParseNumber(Match match) =>
        match.Success && double.TryParse(match.Groups["number"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private sealed record NeedProfile(ProductComparisonNeedOption Option, IReadOnlyDictionary<string, int> Weights);
    private sealed record ComparisonCriterion(string NeedMetric, int WinnerProductId, string Text);
}
