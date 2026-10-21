using EcommerceApp.Controllers;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace EcommerceApp.Tests;

[Collection("SqlServer")]
public sealed class WishlistImageTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Wishlist_cards_use_the_saved_product_image()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var imageUrl = "https://example.test/products/phone.jpg";
        var product = new Product
        {
            Name = "Wishlist image test",
            Description = "Product with a saved image",
            Price = 1_000_000,
            Stock = 1,
            Category = new Category { Name = "Phones", Slug = Guid.NewGuid().ToString("N") },
            Images = new List<ProductImage> { new() { ImageUrl = imageUrl, SortOrder = 0 } }
        };
        db.WishlistItems.Add(new WishlistItem { UserId = user.Id, Product = product });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "Test"))
        };
        var controller = new WishlistController(db, Mock.Of<IProductInteractionService>(), Mock.Of<ISprintFeatureService>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<WishlistItem>>(result.Model));
        Assert.Equal(imageUrl, item.Product!.PrimaryImageUrl);
    }
}
