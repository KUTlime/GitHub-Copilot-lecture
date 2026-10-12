namespace AgentShop.SharedKernel;

/// <summary>
/// Outcome of an operation that can fail in an expected way (see <see cref="Error"/>).
/// </summary>
public class Result
{
    private readonly Error? _error;

    protected Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(error: null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Outcome of an operation that returns <typeparamref name="TValue"/> on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(error: null) => _value = value;

    private Result(Error error)
        : base(error) => _value = default;

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

#pragma warning disable CA1000 // Static factory members on the generic result are the intended API.
    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error) => new(error);
#pragma warning restore CA1000

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new(error);
}
