namespace AgentShop.SharedKernel;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
}

/// <summary>
/// An expected business failure. Unexpected failures are exceptions, not errors.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}

/// <summary>
/// Validation failure with per-field messages. Mapped to the same 400 ValidationProblem shape
/// as FluentValidation errors, so clients see one validation contract.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation)
{
    public static ValidationError For(string field, string message) =>
        new(new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] });
}
