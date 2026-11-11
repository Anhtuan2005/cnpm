using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Middleware;

public sealed class RequestBodyTooLargeMiddleware
{
    private readonly RequestDelegate _next;

    public RequestBodyTooLargeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Kích thước nội dung gửi vượt quá giới hạn cho phép."
            });
        }
    }
}
