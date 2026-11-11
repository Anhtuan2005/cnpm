using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

/// <summary>Idempotent accessory catalog and explicit phone/accessory compatibility offers.</summary>
public static class PhoneAccessoryCatalog
{
    public const string UniversalEarbudsName = "Tai nghe Bluetooth True Wireless";

    public static async Task EnsureAsync(AppDbContext db)
    {
        var category = await db.Categories.SingleOrDefaultAsync(row => row.Slug == PhoneCatalogScope.AccessoryRootSlug);
        if (category is null)
        {
            category = new Category { Name = "Phụ kiện điện thoại", Slug = PhoneCatalogScope.AccessoryRootSlug,
                Description = "Ốp lưng, tai nghe, sạc, cáp và kính bảo vệ dành cho điện thoại." };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        var definitions = new[]
        {
            new Accessory("Ốp lưng trong suốt iPhone 15 Pro", 99_000, "case-iphone-15-pro.webp", 20,
                "Ốp TPU trong suốt dành riêng cho iPhone 15 Pro.", "iPhone 15 Pro", "case.svg"),
            new Accessory("Ốp lưng Samsung Galaxy S24", 119_000, "case-galaxy-s24.webp", 20,
                "Ốp bảo vệ dành riêng cho Samsung Galaxy S24 bản tiêu chuẩn.", "Samsung Galaxy S24", "case-s24.svg"),
            new Accessory(UniversalEarbudsName, 499_000, "earbuds.svg", 15,
                "Tai nghe không dây kèm hộp sạc, kết nối qua Bluetooth với điện thoại. Các tính năng điều khiển tùy thiết bị.", null),
            new Accessory("Củ sạc USB-C PD 20W", 249_000, "charger-20w.webp", 15,
                "Củ sạc USB-C Power Delivery tối đa 20W. Dùng cáp phù hợp với điện thoại. Công suất thực tế tùy máy.", null, "charger.svg"),
            new Accessory("Cáp USB-C sang USB-C 1m", 99_000, "cable-usb-c.webp", 10,
                "Cáp sạc dài 1m với hai đầu USB-C, dành cho điện thoại có cổng USB-C và củ sạc USB-C.", "usb-c", "cable.svg"),
            new Accessory("Cáp USB-C sang Lightning 1m", 149_000, "cable-lightning.webp", 10,
                "Cáp sạc dài 1m, đầu USB-C kết nối củ sạc và đầu Lightning dành cho iPhone 14.", "iPhone 14", "cable.svg"),
            new Accessory("Kính cường lực iPhone 15 Pro", 79_000, "glass-iphone-15-pro.webp", 10,
                "Kính bảo vệ màn hình cắt theo kích thước iPhone 15 Pro.", "iPhone 15 Pro", "screen-protector.svg"),
            new Accessory("Kính cường lực Samsung Galaxy S24", 89_000, "glass-galaxy-s24.webp", 10,
                "Kính bảo vệ màn hình dành cho Samsung Galaxy S24 bản tiêu chuẩn.", "Samsung Galaxy S24"),
            new Accessory("Củ sạc GaN USB-C PD/PPS 100W", 699_000, "charger-100w.webp", 8,
                "Củ sạc USB-C GaN hỗ trợ Power Delivery và PPS, tổng công suất tối đa 100W. Điện thoại nhận công suất theo chuẩn sạc của máy, cáp và số cổng đang dùng.", "pd-charger")
        };
        var seeded = new List<(Accessory Definition, Product Product)>();
        foreach (var definition in definitions)
        {
            var imageUrl = $"/images/accessories/{definition.Image}";
            var product = await db.Products.IgnoreQueryFilters().Include(row => row.Images)
                .OrderBy(row => row.Id).FirstOrDefaultAsync(row => row.Name == definition.Name);
            if (product is null)
            {
                product = new Product { Name = definition.Name, Description = definition.Description,
                    Price = definition.Price, Stock = 50, Category = category,
                    Images = [new ProductImage { ImageUrl = imageUrl, SortOrder = 0 }] };
                db.Products.Add(product);
            }
            else if (definition.LegacyImage is { } legacyImage)
            {
                var legacyUrl = $"/images/accessories/{legacyImage}";
                foreach (var image in product.Images.Where(image => image.ImageUrl == legacyUrl))
                    image.ImageUrl = imageUrl;
            }
            seeded.Add((definition, product));
        }
        await db.SaveChangesAsync();

        var phones = await PhoneCatalogScope.OnlyPhones(db.Products).ToListAsync();
        var usbCPhoneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "iPhone 15 Pro", "Samsung Galaxy S24", "Samsung Galaxy A55 5G", "Xiaomi 14", "Redmi Note 13 Pro 5G",
            "OPPO Reno 11", "OPPO A98 5G", "vivo V30", "vivo Y36", "realme 12 Pro", "realme C67",
            "Google Pixel 8", "Google Pixel 7 Pro", "Sony Xperia 1 V", "Sony Xperia 5 V"
        };
        var pdChargerPhoneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "iPhone 15 Pro", "iPhone 14", "Samsung Galaxy S24", "Samsung Galaxy A55 5G"
        };
        var phoneIds = phones.Select(phone => phone.Id).ToList();
        var accessoryIds = await PhoneCatalogScope.OnlyAccessories(db.Products).Select(product => product.Id).ToListAsync();
        var obsolete = await db.CrossSellOffers.Where(offer => offer.IsActive
            && phoneIds.Contains(offer.AnchorProductId) && !accessoryIds.Contains(offer.AddOnProductId)).ToListAsync();
        foreach (var offer in obsolete) offer.IsActive = false;

        foreach (var phone in phones)
        foreach (var (definition, product) in seeded)
        {
            var compatible = definition.CompatiblePhone switch
            {
                null => true,
                "usb-c" => usbCPhoneNames.Contains(phone.Name),
                "pd-charger" => pdChargerPhoneNames.Contains(phone.Name),
                _ => string.Equals(phone.Name, definition.CompatiblePhone, StringComparison.OrdinalIgnoreCase)
            };
            if (!compatible) continue;
            if (await db.CrossSellOffers.AnyAsync(offer => offer.AnchorProductId == phone.Id && offer.AddOnProductId == product.Id))
                continue; // Preserve an administrator's price, availability, and offer overrides.
            db.CrossSellOffers.Add(new CrossSellOffer { AnchorProductId = phone.Id, AddOnProductId = product.Id,
                DiscountPercent = definition.Discount, IsActive = true });
        }
        await db.SaveChangesAsync();
    }

    private sealed record Accessory(
        string Name, decimal Price, string Image, int Discount, string Description, string? CompatiblePhone, string? LegacyImage = null);
}
