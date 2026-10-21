using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EcommerceApp.Tests;

public sealed class VnpayServiceTests
{
    private const string TestSecret = "sandbox-test-secret";

    [Fact]
    public void Payment_url_uses_the_configured_terminal_and_a_valid_checksum()
    {
        var service = CreateService();
        var order = new Order
        {
            Id = 42,
            TotalAmount = 1_490_000,
            PaymentExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        var paymentUrl = service.CreatePaymentUrl(order, new DefaultHttpContext());
        var uri = new Uri(paymentUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);

        Assert.Equal("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", uri.GetLeftPart(UriPartial.Path));
        Assert.Equal("TESTCODE", query["vnp_TmnCode"].ToString());
        Assert.Equal("149000000", query["vnp_Amount"].ToString());
        Assert.Equal("http://localhost:5009/payment/vnpay-return", query["vnp_ReturnUrl"].ToString());
        Assert.Equal(Sign(new QueryCollection(query)), query["vnp_SecureHash"].ToString());
    }

    [Fact]
    public void Callback_accepts_a_signed_payment_and_rejects_a_changed_amount()
    {
        var service = CreateService();
        var fields = new Dictionary<string, StringValues>
        {
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TxnRef"] = "42_20261014220000",
            ["vnp_Amount"] = "149000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TransactionNo"] = "12345"
        };
        fields["vnp_SecureHash"] = Sign(new QueryCollection(fields));

        var valid = service.ProcessCallback(new QueryCollection(fields));
        Assert.True(valid.IsSignatureValid);
        Assert.True(valid.IsSuccess);
        Assert.Equal("42", valid.OrderId);
        Assert.Equal(1_490_000, valid.Amount);

        fields["vnp_Amount"] = "149000100";
        var changed = service.ProcessCallback(new QueryCollection(fields));
        Assert.False(changed.IsSignatureValid);
        Assert.False(changed.IsSuccess);
    }

    private static VnpayService CreateService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vnpay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ["Vnpay:TmnCode"] = "TESTCODE",
            ["Vnpay:HashSecret"] = TestSecret,
            ["Vnpay:ReturnUrl"] = "http://localhost:5009/payment/vnpay-return"
        }).Build();
        return new VnpayService(configuration);
    }

    private static string Sign(IQueryCollection query)
    {
        var canonical = string.Join("&", query
            .Where(pair => pair.Key.StartsWith("vnp_", StringComparison.Ordinal)
                && pair.Key is not "vnp_SecureHash" and not "vnp_SecureHashType")
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value.ToString())}"));
        return Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(TestSecret), Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
    }
}
