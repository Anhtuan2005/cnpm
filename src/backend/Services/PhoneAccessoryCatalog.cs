using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

/// <summary>Idempotent accessory catalog and explicit phone/accessory compatibility offers.</summary>
public static class PhoneAccessoryCatalog
{
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
            new Accessory("Ốp lưng trong suốt iPhone 15 Pro", 99_000, "case", 20,
                "Ốp TPU trong suốt dành riêng cho iPhone 15 Pro. Không dùng cho các mẫu iPhone khác.", "iPhone 15 Pro"),
            new Accessory("Ốp lưng Samsung Galaxy S24", 119_000, "case-s24", 20,
                "Ốp bảo vệ dành riêng cho Samsung Galaxy S24 bản tiêu chuẩn. Không dùng cho bản Plus hoặc Ultra.", "Samsung Galaxy S24"),
            new Accessory("Tai nghe Bluetooth True Wireless", 499_000, "earbuds", 15,
                "Tai nghe không dây kèm hộp sạc, kết nối qua Bluetooth với điện thoại. Các tính năng điều khiển tùy thiết bị.", null),
            new Accessory("Củ sạc USB-C PD 20W", 249_000, "charger", 15,
                "Củ sạc có cổng ra USB-C, hỗ trợ USB Power Delivery tối đa 20W. Cần cáp phù hợp với cổng của điện thoại; công suất thực tế tùy máy.", null),
            new Accessory("Cáp USB-C sang USB-C 1m", 99_000, "cable", 10,
                "Cáp sạc dài 1m với hai đầu USB-C, dành cho điện thoại có cổng USB-C và củ sạc USB-C.", "usb-c"),
            new Accessory("Cáp USB-C sang Lightning 1m", 149_000, "cable", 10,
                "Cáp sạc dài 1m, đầu USB-C kết nối củ sạc và đầu Lightning dành cho iPhone 14.", "iPhone 14"),
            new Accessory("Kính cường lực iPhone 15 Pro", 79_000, "screen-protector", 10,
                "Kính bảo vệ màn hình cắt theo kích thước iPhone 15 Pro. Không dùng cho các kích thước máy khác.", "iPhone 15 Pro")
        };
        var seeded = new List<(Accessory Definition, Product Product)>();
        foreach (var definition in definitions)
        {
            var product = await db.Products.IgnoreQueryFilters().Include(row => row.Images)
                .OrderBy(row => row.Id).FirstOrDefaultAsync(row => row.Name == definition.Name);
            if (product is null)
            {
                product = new Product { Name = definition.Name, Description = definition.Description,
                    Price = definition.Price, Stock = 50, Category = category,
                    Images = [new ProductImage { ImageUrl = $"/images/accessories/{definition.Image}.svg", SortOrder = 0 }] };
                db.Products.Add(product);
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
        var phoneIds = phones.Select(phone => phone.Id).ToList();
        var accessoryIds = await PhoneCatalogScope.OnlyAccessories(db.Products).Select(product => product.Id).ToListAsync();
        var obsolete = await db.CrossSellOffers.Where(offer => offer.IsActive
            && phoneIds.Contains(offer.AnchorProductId) && !accessoryIds.Contains(offer.AddOnProductId)).ToListAsync();
        foreach (var offer in obsolete) offer.IsActive = false;

        foreach (var phone in phones)
        foreach (var (definition, product) in seeded)
        {
            if (definition.CompatiblePhone is not null
                && (definition.CompatiblePhone == "usb-c" ? !usbCPhoneNames.Contains(phone.Name) : phone.Name != definition.CompatiblePhone))
                continue;
            if (await db.CrossSellOffers.AnyAsync(offer => offer.AnchorProductId == phone.Id && offer.AddOnProductId == product.Id))
                continue; // Preserve an administrator's price, availability, and offer overrides.
            db.CrossSellOffers.Add(new CrossSellOffer { AnchorProductId = phone.Id, AddOnProductId = product.Id,
                DiscountPercent = definition.Discount, IsActive = true });
        }
        await db.SaveChangesAsync();
    }

    private sealed record Accessory(string Name, decimal Price, string Image, int Discount, string Description, string? CompatiblePhone);
}
