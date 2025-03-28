public static class PathHelper
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="path"></param>
    /// <param name="offset"></param>
    /// <param name="nextOffset"></param>
    public static ReadOnlySpan<char> GetNextSegment(ReadOnlySpan<char> path, int offset, out int nextOffset)
    {
        if (offset < 0 || offset >= path.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        var segment = path[offset..];
        var periodOffset = segment.IndexOf('.');
        var bracketOffset = segment.IndexOf('[');
        var endOffset = (periodOffset, bracketOffset) switch
        {
            (-1, -1) => -1,
            (-1, _) => bracketOffset,
            (_, -1) => periodOffset,
            _ => Math.Min(periodOffset, bracketOffset),
        };
        if (endOffset == -1)
        {
            nextOffset = -1;
            return segment;
        }
        nextOffset = endOffset + offset + 1;

        return segment[..endOffset];
    }

    public static int GetIndex(ReadOnlySpan<char> path, int offset, out int nextOffset)
    {
        var segment = path[offset..];
        var bracketOffset = segment.IndexOf(']');
        if (bracketOffset < 0)
        {
            throw new IndexOutOfRangeException();
        }

        if (!int.TryParse(segment[..bracketOffset], out var index))
        {
            throw new IndexOutOfRangeException();
        }

        nextOffset = offset + bracketOffset + 2;
        if (nextOffset >= path.Length)
        {
            nextOffset = -1;
        }

        return index;
    }
}
