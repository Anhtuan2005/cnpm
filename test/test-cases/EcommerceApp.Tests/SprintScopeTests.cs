using System.Reflection;
using EcommerceApp.Controllers;
using EcommerceApp.Controllers.Admin;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceApp.Tests;

public class SprintScopeTests
{
    [Fact]
    public void Product_comparison_is_available_in_sprint_two()
    {
        var action = typeof(ProductController).GetMethod("Compare", BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Equal(2, action!.GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
    }

    [Theory]
    [InlineData(nameof(OrderController.Confirmation))]
    [InlineData(nameof(OrderController.History))]
    [InlineData(nameof(OrderController.Detail))]
    public void Sprint_one_order_read_pages_are_available(string actionName)
    {
        var action = typeof(OrderController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Null(action!.GetCustomAttribute<MinimumSprintAttribute>());
    }

    [Theory]
    [InlineData(nameof(OrderController.Cancel))]
    [InlineData(nameof(OrderController.Reorder))]
    public void Sprint_two_order_actions_are_guarded_at_sprint_two(string actionName)
    {
        var action = typeof(OrderController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Equal(2, action!.GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
    }

    [Theory]
    [InlineData(nameof(InfoController.About))]
    [InlineData(nameof(InfoController.Privacy))]
    [InlineData(nameof(InfoController.Terms))]
    [InlineData(nameof(InfoController.Careers))]
    [InlineData(nameof(InfoController.BuyingGuide))]
    [InlineData(nameof(InfoController.Returns))]
    [InlineData(nameof(InfoController.Warranty))]
    [InlineData(nameof(InfoController.Faq))]
    public void Sprint_two_information_pages_are_guarded_at_sprint_two(string actionName)
    {
        var action = typeof(InfoController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Equal(2, action!.GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
    }

    [Fact]
    public void Customer_only_areas_exclude_admin_accounts()
    {
        Assert.Equal("User", typeof(OrderController).GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal("User", typeof(WishlistController).GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Fact]
    public void Sprint_one_admin_catalog_management_is_available()
    {
        Assert.Null(typeof(AdminProductController).GetCustomAttribute<MinimumSprintAttribute>());
        Assert.Null(typeof(AdminCategoryController).GetCustomAttribute<MinimumSprintAttribute>());
        Assert.Null(typeof(AdminBannerController).GetCustomAttribute<MinimumSprintAttribute>());
    }

    [Fact]
    public void Sprint_two_controllers_are_guarded_at_sprint_two_and_later_features_remain_guarded()
    {
        var guardedControllers = new (Type Type, int Sprint)[]
        {
            (typeof(PaymentController), 2),
            (typeof(GhnWebhookController), 2),
            (typeof(NotificationController), 2),
            (typeof(ReviewController), 2),
            (typeof(ReturnWarrantyController), 2),
            (typeof(VoucherController), 2),
            (typeof(AiChatController), 3),
            (typeof(SeoController), 3),
            (typeof(BuildPcController), 4),
            (typeof(AdminDashboardController), 2),
            (typeof(AdminNotificationController), 2),
            (typeof(AdminOrderController), 2),
            (typeof(AdminShippingController), 2),
            (typeof(AdminReviewController), 2),
            (typeof(AdminVoucherController), 3),
            (typeof(AdminReturnWarrantyController), 3),
            (typeof(AdminCustomerSegmentController), 3),
            (typeof(AdminReportController), 3),
            (typeof(AdminUserController), 3)
        };

        foreach (var (controller, sprint) in guardedControllers)
        {
            Assert.Equal(sprint, controller.GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
        }
    }

    [Fact]
    public void Actions_keep_their_intended_sprint_boundaries()
    {
        Assert.Equal(3, typeof(ProductController).GetMethod(nameof(ProductController.Track))?
            .GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
        Assert.Equal(2, typeof(AdminProductController).GetMethod(nameof(AdminProductController.AdjustStock))?
            .GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
        Assert.Equal(2, typeof(HomeController).GetMethod(nameof(HomeController.Privacy))?
            .GetCustomAttribute<MinimumSprintAttribute>()?.Sprint);
    }

    [Fact]
    public void Removed_catalog_extras_are_not_part_of_the_product_service()
    {
        Assert.Null(typeof(IProductService).GetMethod("GetLatestProductsAsync"));
        Assert.Null(typeof(IProductService).GetMethod("GetRelatedProductsAsync"));
    }

    [Fact]
    public void Order_history_has_paging_without_an_extra_status_filter()
    {
        var history = typeof(OrderController).GetMethod(nameof(OrderController.History));

        Assert.NotNull(history);
        Assert.Equal(new[] { "page" }, history!.GetParameters().Select(parameter => parameter.Name));
    }

    [Fact]
    public void Required_sprint_two_user_story_actions_are_present()
    {
        var requiredActions = new (Type Controller, string[] Actions)[]
        {
            (typeof(HomeController), [nameof(HomeController.Index)]),
            (typeof(ProductController), [nameof(ProductController.Index), nameof(ProductController.Search), nameof(ProductController.Detail), nameof(ProductController.Compare)]),
            (typeof(AccountController), [nameof(AccountController.Register), nameof(AccountController.Login), nameof(AccountController.Logout), nameof(AccountController.Profile)]),
            (typeof(WishlistController), [nameof(WishlistController.Index), nameof(WishlistController.Toggle)]),
            (typeof(CartController), [nameof(CartController.Index), nameof(CartController.Add), nameof(CartController.Update), nameof(CartController.Remove)]),
            (typeof(OrderController), [nameof(OrderController.Checkout), nameof(OrderController.ShippingFee), nameof(OrderController.Confirmation), nameof(OrderController.History), nameof(OrderController.Detail), nameof(OrderController.Cancel), nameof(OrderController.Reorder)]),
            (typeof(PaymentController), [nameof(PaymentController.VnpayCreate), nameof(PaymentController.VnpayReturn), nameof(PaymentController.VnpayIpn)]),
            (typeof(VoucherController), [nameof(VoucherController.Apply), nameof(VoucherController.Validate)]),
            (typeof(ReviewController), [nameof(ReviewController.Submit)]),
            (typeof(ReturnWarrantyController), [nameof(ReturnWarrantyController.Index), nameof(ReturnWarrantyController.Create), nameof(ReturnWarrantyController.Details)]),
            (typeof(NotificationController), [nameof(NotificationController.Index), nameof(NotificationController.Go), nameof(NotificationController.MarkAllRead)]),
            (typeof(AdminProductController), [nameof(AdminProductController.Index), nameof(AdminProductController.Create), nameof(AdminProductController.Edit), nameof(AdminProductController.Delete), nameof(AdminProductController.AdjustStock)]),
            (typeof(AdminCategoryController), [nameof(AdminCategoryController.Index), nameof(AdminCategoryController.Create), nameof(AdminCategoryController.Edit), nameof(AdminCategoryController.Delete)]),
            (typeof(AdminBannerController), [nameof(AdminBannerController.Index), nameof(AdminBannerController.Create), nameof(AdminBannerController.Edit), nameof(AdminBannerController.Delete)]),
            (typeof(AdminOrderController), [nameof(AdminOrderController.Index), nameof(AdminOrderController.Details), nameof(AdminOrderController.UpdateStatus), nameof(AdminOrderController.MarkRefunded), nameof(AdminOrderController.IssueInvoice), nameof(AdminOrderController.ExportCsv)]),
            (typeof(AdminShippingController), [nameof(AdminShippingController.Index), nameof(AdminShippingController.UpdateStatus), nameof(AdminShippingController.GhnCreate), nameof(AdminShippingController.GhnSync), nameof(AdminShippingController.GhnCancel)]),
            (typeof(AdminReviewController), [nameof(AdminReviewController.Index), nameof(AdminReviewController.Approve), nameof(AdminReviewController.Reject), nameof(AdminReviewController.Delete)]),
            (typeof(AdminDashboardController), [nameof(AdminDashboardController.Index)])
        };

        foreach (var (controller, actions) in requiredActions)
        {
            var exposedActions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select(method => method.Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var action in actions)
            {
                Assert.Contains(action, exposedActions);
            }
        }

        Assert.NotNull(typeof(ICartService).GetMethod("MergeGuestCartAsync"));
        Assert.NotNull(typeof(IShippingFeeService).GetMethod(nameof(IShippingFeeService.Calculate)));
    }
}
