using EcommerceApp.Controllers;
using EcommerceApp.Controllers.Admin;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace EcommerceApp.Tests;

[Collection("SqlServer")]
public sealed class ReviewManagementTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Customer_review_is_published_immediately_and_store_can_reply()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SqlServerFixture.CreateUserAsync(scope.ServiceProvider);
        var product = new Product
        {
            Name = "Review management phone",
            Description = "Test phone",
            Price = 1_000_000,
            Stock = 1,
            Category = new Category { Name = "Phones", Slug = Guid.NewGuid().ToString("N") }
        };
        db.Orders.Add(new Order
        {
            UserId = user.Id,
            Status = OrderStatuses.Delivered,
            RecipientName = "Test customer",
            RecipientPhone = "0900000000",
            ShippingAddress = "Test address",
            Items = new List<OrderItem> { new() { Product = product, Quantity = 1, UnitPrice = product.Price } }
        });
        await db.SaveChangesAsync();

        var controller = new ReviewController(db, Mock.Of<IImageStorageService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Role, "User") },
                        "Test"))
                }
            }
        };
        var response = Assert.IsType<JsonResult>(await controller.Submit(
            product.Id, 5, "Điện thoại hoạt động rất tốt.", null));
        var payload = JsonSerializer.Serialize(response.Value);
        using var document = JsonDocument.Parse(payload);
        Assert.Contains("Cảm ơn", document.RootElement.GetProperty("message").GetString());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());

        var review = await db.Reviews.SingleAsync(row => row.UserId == user.Id && row.ProductId == product.Id);
        Assert.Equal("Điện thoại hoạt động rất tốt.", review.Comment);

        var adminHttpContext = new DefaultHttpContext();
        var adminController = new AdminReviewController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = adminHttpContext },
            TempData = new TempDataDictionary(adminHttpContext, Mock.Of<ITempDataProvider>())
        };
        var replyResponse = Assert.IsType<RedirectToActionResult>(await adminController.Reply(
            review.Id, "Cảm ơn bạn đã chia sẻ trải nghiệm."));
        Assert.Equal(nameof(AdminReviewController.Index), replyResponse.ActionName);

        await db.Entry(review).ReloadAsync();
        Assert.Equal("Cảm ơn bạn đã chia sẻ trải nghiệm.", review.StoreReply);
        Assert.NotNull(review.StoreRepliedAt);
    }
}
