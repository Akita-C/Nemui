namespace Nemui.Shared.Exceptions;

public abstract class BaseCustomException : Exception
{
    public string ErrorCode { get; protected set; }
    public DateTime OccurredAt { get; protected set; }

    protected BaseCustomException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
        OccurredAt = DateTime.UtcNow;
    }

    protected BaseCustomException(string message, Exception innerException, string errorCode) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        OccurredAt = DateTime.UtcNow;
    }
}