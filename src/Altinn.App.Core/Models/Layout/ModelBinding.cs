namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Wrapper type for a model binding with optional data type specification
/// </summary>
public readonly record struct ModelBinding
{
    /// <summary>
    /// The field in the model the binding is for
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// The data type the binding refers to (default model for layout if null)
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Implicit conversion from string to <see cref="ModelBinding" /> for
    /// bacwards convenicence
    /// </summary>
    public static implicit operator ModelBinding(string field)
    {
        return new ModelBinding { Field = field, };
    }
}
