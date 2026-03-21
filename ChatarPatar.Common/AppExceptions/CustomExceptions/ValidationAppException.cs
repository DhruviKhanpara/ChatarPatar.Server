using FluentValidation.Results;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class ValidationAppException : AppException
{
    public List<ValidationError> Errors { get; }

    public ValidationAppException(IEnumerable<ValidationFailure> failures)
        : base("Validation failed for one or more properties.")
    {
        Errors = failures.Select(err => new ValidationError(err.PropertyName, err.ErrorMessage)).ToList();
    }

    public IReadOnlyDictionary<string, string[]> GroupedErrors =>
        Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}

public record ValidationError(string PropertyName, string ErrorMessage);