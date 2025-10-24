namespace ProductManagement.Exceptions;

public class ValidationException : BaseException
{
    public List<string> Errors { get; }
    public ValidationException(string message, List<string> errors) 
        : base(message, 400, "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}