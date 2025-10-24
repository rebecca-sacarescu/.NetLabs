using System.Net;
using System.Text.Json;
using ProductManagement.Exceptions;

namespace ProductManagement.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ErrorResponse errorResponse;
        int statusCode;

        switch (exception)
        {
            case ValidationException validationEx:
                errorResponse = new ErrorResponse(validationEx.ErrorCode, validationEx.Message, validationEx.Errors)
                {
                    TraceId = context.TraceIdentifier
                };
                statusCode = validationEx.StatusCode;
                break;
                
            case BaseException baseEx:
                errorResponse = new ErrorResponse(baseEx.ErrorCode, baseEx.Message)
                {
                    TraceId = context.TraceIdentifier
                };
                statusCode = baseEx.StatusCode;
                break;
                
            default:
                errorResponse = new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred")
                {
                    TraceId = context.TraceIdentifier
                };
                statusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.StatusCode = statusCode;
        var response = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(response);
    }
}