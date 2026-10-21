using System.Text.Json;
using EcommerceApp.Models.ViewModels;

namespace EcommerceApp.Services;

public static class PhoneSpecificationFields
{
    public const string Unknown = "Chưa cập nhật";
    public sealed record Field(string Key, string Label, string Example);
    public static readonly IReadOnlyList<Field> All = new[]
    {
        new Field("brand", "Hãng sản xuất", "Samsung"),
        new Field("warranty", "Bảo hành", "12 tháng"),
        new Field("display", "Màn hình", "6.6 inch Super AMOLED, 120Hz"),
        new Field("chip", "Chip xử lý", "Exynos 1480"),
        new Field("memory", "RAM / Bộ nhớ", "8GB RAM, 256GB lưu trữ"),
        new Field("os", "Hệ điều hành", "Android, One UI"),
        new Field("battery", "Pin và sạc", "5000mAh, sạc nhanh 25W"),
        new Field("rearCamera", "Camera sau", "50MP chính OIS, 12MP góc siêu rộng, 5MP macro"),
        new Field("mainCamera", "Camera chính", "50MP, OIS"),
        new Field("frontCamera", "Camera trước", "32MP"),
        new Field("telephoto", "Camera telephoto", "Không có"),
        new Field("ultrawide", "Camera góc siêu rộng", "12MP"),
        new Field("material", "Chất liệu", "Khung kim loại, mặt lưng kính")
    };

    public static string Serialize(IReadOnlyDictionary<string, string?> values) => JsonSerializer.Serialize(
        All.Where(field => values.TryGetValue(field.Key, out var value) && !string.IsNullOrWhiteSpace(value))
            .ToDictionary(field => field.Label, field => values[field.Key]!.Trim()));

    public static Dictionary<string, string?> ToForm(IEnumerable<ProductSpecViewModel> specs)
    {
        var values = specs.ToDictionary(spec => spec.Label, spec => spec.Value);
        return All.ToDictionary(field => field.Key, field =>
            values.TryGetValue(field.Label, out var value) && value != Unknown ? value : null);
    }

    public static IReadOnlyList<ProductSpecViewModel> Read(string json)
    {
        Dictionary<string, string?> values;
        try { values = JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new(); }
        catch (JsonException) { values = new(); }
        return All.Select((field, index) => (field, index))
            .Where(entry => entry.index < 8 || !string.IsNullOrWhiteSpace(values.GetValueOrDefault(entry.field.Label)))
            .Select(entry => new ProductSpecViewModel { Label = entry.field.Label,
                Value = string.IsNullOrWhiteSpace(values.GetValueOrDefault(entry.field.Label)) ? Unknown : values[entry.field.Label]! })
            .ToList();
    }
}
