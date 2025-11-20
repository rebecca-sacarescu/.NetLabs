namespace ProductManagement.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeaderName, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationHeaderName] = correlationId;
        }

        context.Response.Headers[CorrelationHeaderName] = correlationId!;

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["CorrelationId"] = correlationId!.ToString()
               }))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}