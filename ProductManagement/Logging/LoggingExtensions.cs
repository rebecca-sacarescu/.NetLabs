using Microsoft.Extensions.Logging;
using ProductManagement.Metrics;

namespace ProductManagement.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(
        this ILogger logger,
        ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            new EventId(LogEvents.ProductCreationCompleted, nameof(LogEvents.ProductCreationCompleted)),
            "ProductCreationMetrics | OperationId: {OperationId}, " +
            "Name: {ProductName}, SKU: {SKU}, Category: {Category}, " +
            "ValidationDurationMs: {ValidationDurationMs}, " +
            "DatabaseSaveDurationMs: {DatabaseSaveDurationMs}, " +
            "TotalDurationMs: {TotalDurationMs}, " +
            "Success: {Success}, ErrorReason: {ErrorReason}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category.ToString(),
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? string.Empty);
    }
}