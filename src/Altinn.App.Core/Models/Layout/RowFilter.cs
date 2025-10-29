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
    /// The stopping index (exclusive) of rows to show. Rows at or after this index are hidden.
    /// </summary>
    public required int Stop { get; init; }

    /// <summary>
    /// Determines whether the specified row index should be visible based on the filter.
    /// </summary>
    /// <param name="rowIndex">The zero-based index of the row to check.</param>
    /// <returns>True if the row should be visible; false if it should be hidden.</returns>
    public bool IsRowVisible(int rowIndex)
    {
        return rowIndex >= Start && rowIndex < Stop;
    }
}
