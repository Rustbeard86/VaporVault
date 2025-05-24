namespace VaporVault.Core.Common;

public class OperationResult<T>
{
    private OperationResult(bool success, T data, string errorMessage)
    {
        Success = success;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }
    public string ErrorMessage { get; }
    public T Data { get; }

    public static OperationResult<T> CreateSuccess(T data)
    {
        return new OperationResult<T>(true, data, string.Empty);
    }

    public static OperationResult<T> CreateFailure(string errorMessage)
    {
        return new OperationResult<T>(false, default!, errorMessage);
    }
}