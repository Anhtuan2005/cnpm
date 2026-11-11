namespace EcommerceApp.Models.ViewModels;

public sealed record AccessorySuggestion(
    int ProductId, string ProductName, string PhoneName, string ImageUrl, decimal RetailPrice);
