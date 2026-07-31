namespace Philobiblos.Application.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Dictionary<string, string[]> Errors { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Errors = [];
    }

    private Result(Dictionary<string, string[]> errors)
    {
        IsSuccess = false;
        Value = default;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Dictionary<string, string[]> errors) => new(errors);
}
