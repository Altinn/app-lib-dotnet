namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Represents a filter for row indices in repeating components.
/// Rows with indices less than <see cref="Start"/> or greater than or equal to <see cref="Stop"/> are hidden.
/// </summary>
internal sealed class RowFilter
{
    /// <summary>
    /// The starting index (inclusive) of rows to show. Rows before this index are hidden.
    /// </summary>
    public required int Start { get; init; }

    /// <summary>
    /// The stopping index (inclusive) of rows to show. Rows after this index are hidden.
    /// </summary>
    public required int Stop { get; init; }

    /// <summary>
    /// Determines whether the specified row index is within the filter range.
    /// </summary>
    /// <param name="rowIndex">The zero-based index of the row to check.</param>
    /// <returns>True if the row is within the filter range; false otherwise.</returns>
    public bool IsInRange(int rowIndex)
    {
        return rowIndex >= Start && rowIndex <= Stop;
    }
}
