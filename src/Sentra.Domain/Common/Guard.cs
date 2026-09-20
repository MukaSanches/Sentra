namespace Sentra.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    public static string? Optional(string? value, string parameterName, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (normalized is not null && normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    public static Guid RequiredId(Guid value, string parameterName)
        => value == Guid.Empty
            ? throw new ArgumentException("Identifier is required.", parameterName)
            : value;
}
