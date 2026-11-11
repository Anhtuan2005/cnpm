using EcommerceApp.Middleware;
using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Tests;

public class RequestBodyTooLargeMiddlewareTests
{
    [Fact]
    public async Task Kestrel_oversize_exception_is_returned_as_413()
    {
        var middleware = new RequestBodyTooLargeMiddleware(_ =>
            throw new BadHttpRequestException(
                "Request body too large.", StatusCodes.Status413PayloadTooLarge));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        Assert.Contains("Kích thước nội dung gửi vượt quá giới hạn", await reader.ReadToEndAsync());
    }
}
