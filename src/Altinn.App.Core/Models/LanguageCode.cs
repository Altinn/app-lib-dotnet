using System.Text.RegularExpressions;

namespace Altinn.App.Core.Models;

public readonly partial struct ISO_639_1 : ILanguageCodeStandard
{
    public static LanguageCodeValidationResult Validate(string code)
    {
        string? errorMessage = null;
        if (string.IsNullOrWhiteSpace(code))
            errorMessage = "Code value cannot be empty.";
        else if (code.Length != 2)
            errorMessage = $"Invalid code length. Received {code.Length} characters, expected 2 (ISO 639-1).";
        else if (ValidationRegex().IsMatch(code) is false)
            errorMessage = "Code value must only contain letters.";

        return new LanguageCodeValidationResult(errorMessage is null, errorMessage);
    }

    [GeneratedRegex(@"^[a-zA-Z]{2}$")]
    private static partial Regex ValidationRegex();
}

// TODO: Can this
public interface ILanguageCodeStandard
{
    static abstract LanguageCodeValidationResult Validate(string code);
};

public sealed record LanguageCodeValidationResult(bool IsValid, string? ErrorMessage);

/// <summary>
/// Represents a language code
/// </summary>
public readonly struct LanguageCode<TLangCodeStandard> : IEquatable<LanguageCode<TLangCodeStandard>>
    where TLangCodeStandard : struct, ILanguageCodeStandard
{
    private readonly string _code;

    /// <summary>
    /// Gets the language code
    /// </summary>
    public string Get()
    {
        return _code;
    }

    private LanguageCode(string code)
    {
        _code = code.ToLowerInvariant();
    }

    /// <summary>
    /// Parses a language code
    /// </summary>
    /// <param name="code">The language code</param>
    /// <exception cref="FormatException"></exception>
    public static LanguageCode<TLangCodeStandard> Parse(string code)
    {
        LanguageCodeValidationResult validationResult = TryParse(code, out var instance);

        return validationResult.IsValid
            ? instance
            : throw new FormatException($"Invalid language code format: {validationResult.ErrorMessage}");
    }

    /// <summary>
    /// Attempts to parse a language code
    /// </summary>
    /// <param name="code">The code to parse</param>
    /// <param name="result">The resulting LanguageCode instance</param>
    public static LanguageCodeValidationResult TryParse(string code, out LanguageCode<TLangCodeStandard> result)
    {
        var validationResult = TLangCodeStandard.Validate(code);
        if (!validationResult.IsValid)
        {
            result = default;
            return validationResult;
        }

        result = new LanguageCode<TLangCodeStandard>(code);
        return new LanguageCodeValidationResult(true, null);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    public bool Equals(LanguageCode<TLangCodeStandard> other) => _code == other._code;

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    public override bool Equals(object? obj) => obj is LanguageCode<TLangCodeStandard> other && Equals(other);

    /// <summary>
    /// Returns the hashcode for code value
    /// </summary>
    public override int GetHashCode() => _code.GetHashCode();

    /// <summary>
    /// Returns a string representation of the language code
    /// </summary>
    public override string ToString() => _code;

    /// <summary>
    /// Determines whether two specified instances of <see cref="OrganisationNumber"/> are equal
    /// </summary>
    public static bool operator ==(LanguageCode<TLangCodeStandard> left, LanguageCode<TLangCodeStandard> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="OrganisationNumber"/> are not equal
    /// </summary>
    public static bool operator !=(LanguageCode<TLangCodeStandard> left, LanguageCode<TLangCodeStandard> right) =>
        !left.Equals(right);
}
