namespace Nemui.Shared.DTOs.Common;

public class ErrorResponse : BaseApiResponse
{
    public ErrorResponse()
    {
        Success = false;
    }
    
    public static ErrorResponse Create(string message, List<string>? errors = null)
    {
        return new ErrorResponse
        {
            Message = message,
            Errors = errors ?? []
        };
    }
    
    public static ErrorResponse FromValidation(List<string> validationErrors)
    {
        return new ErrorResponse
        {
            Message = "Validation failed",
            Errors = validationErrors
        };
    }
}