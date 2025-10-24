using System.Diagnostics;

namespace ProductManagement.Middleware;

public class ErrorResponse
{
    public ErrorResponse()
    {
        Details = new List<string>();
        TraceId = Activity.Current?.Id ?? string.Empty;
    }
    public ErrorResponse(string errorCode, string message) : this()
    {
        ErrorCode = errorCode;
        Message = message;
    }
    public ErrorResponse(string errorCode, string message, List<string> details) : this(errorCode, message)
    {
        Details = details;
    }
    public List<string> Details { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public string TraceId { get; set; }
}