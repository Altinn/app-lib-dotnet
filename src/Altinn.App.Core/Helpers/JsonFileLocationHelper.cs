using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Custom location object instead of using named tuple
/// </summary>
public readonly record struct Location
{
    public required int LineNumberFrom { get; init; }
    public required int ColumnNumberFrom { get; init; }
    public required int LineNumberTo { get; init; }
    public required int ColumnNumberTo { get; init; }
}

/// <summary>
/// Helper class for finding byte range  of a JSON pointer in a JSON file
/// </summary>
public static class JsonFileLocationHelper
{
    /// <summary>
    /// Read json and find the byte range of a json pointer for possible error reporting
    /// </summary>
    /// <remarks>
    /// * If parts of the pointer is not found, the range of last valid segment is returned
    /// * Use a trailing / to get the range of a property value instead of the property name with trailing :
    /// </remarks>
    /// <param name="bytes">Utf8 encoded json text as bytes</param>
    /// <param name="segments">List of the segments in the pointer</param>
    /// <returns>start and end index of the pointer location</returns>
    public static Range GetByteRangeFromJsonPointerSegments(ReadOnlySpan<byte> bytes, ReadOnlySpan<string> segments)
    {
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        return GetByteRangeFromJsonPointerSegments(ref reader, segments);
    }

    /// <summary>
    /// Get the line location of a byte range in a text by counting line breaks "\n"
    /// </summary>
    /// <param name="bytes">The utf-8 encoded text to search in</param>
    /// <param name="range">A range object with byte indexes for start and end</param>
    /// <returns>The location object with line info for both start and end</returns>
    public static Location GetLineLocation(ReadOnlySpan<byte> bytes, Range range)
    {
        return GetLineLocation(bytes, range.Start.GetOffset(bytes.Length), range.End.GetOffset(bytes.Length));
    }

    /// <summary>
    /// Get the line location of a byte range in a text by counting line breaks "\n"
    /// </summary>
    /// <param name="bytes">The utf-8 encoded text to search in</param>
    /// <param name="start">The first byte index</param>
    /// <param name="end">The last byte index</param>
    /// <returns>The location object with line info for both start and end</returns>
    public static Location GetLineLocation(ReadOnlySpan<byte> bytes, int start, int end)
    {
        int lineNumberFrom = -1;
        int lineNumberTo = -1;
        int columnNumberFrom = -1;
        int columnNumberTo = -1;

        int lineNumber = 0;
        int columnNumber = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == '\n')
            {
                lineNumber++;
                columnNumber = 0;
            }
            else
            {
                columnNumber++;
            }

            if (start == i)
            {
                lineNumberFrom = lineNumber;
                columnNumberFrom = columnNumber;
            }

            if (end == i)
            {
                lineNumberTo = lineNumber;
                columnNumberTo = columnNumber;
            }
        }

        return new Location
        {
            LineNumberFrom = lineNumberFrom,
            LineNumberTo = lineNumberTo,
            ColumnNumberFrom = columnNumberFrom,
            ColumnNumberTo = columnNumberTo
        };
    }

    private static Range GetByteRangeFromJsonPointerSegments(ref Utf8JsonReader reader, ReadOnlySpan<string> segments)
    {
        Debug.Assert(reader.TokenType != JsonTokenType.None);
        Debug.Assert(reader.TokenType != JsonTokenType.PropertyName);
        long bytePosStart = reader.TokenStartIndex;
        switch (segments)
        {
            case []:
            case [""]:
                // Attach the error to the current token if no segments left
                reader.Skip();
                return new((int)bytePosStart, (int)reader.BytesConsumed);
            case [var segment, .. var childSegments]:
                return reader.TokenType switch
                {
                    JsonTokenType.StartObject => ReadObject(ref reader, segment, childSegments),
                    JsonTokenType.StartArray => ReadArray(ref reader, segment, childSegments),
                    // Leaf node, just attach the error to the current token
                    _ => ReadLeafNode(reader),
                };
        }
    }

    private static Range ReadLeafNode(Utf8JsonReader reader)
    {
        Debug.Assert(
            reader.TokenType != JsonTokenType.StartArray
                && reader.TokenType != JsonTokenType.StartObject
                && reader.TokenType != JsonTokenType.EndArray
                && reader.TokenType != JsonTokenType.EndObject
        );
        long bytePosStart = reader.TokenStartIndex;
        return new Range((Index)bytePosStart, (Index)reader.BytesConsumed);
    }

    private static Range ReadArray(ref Utf8JsonReader reader, string segment, ReadOnlySpan<string> childSegments)
    {
        Debug.Assert(reader.TokenType == JsonTokenType.StartArray);
        long bytePosStart = reader.TokenStartIndex;
        if (!int.TryParse(segment, CultureInfo.InvariantCulture, out int index))
        {
            // Index is not valid number. Something is an array, which was not expected to be one.
            reader.Skip();
            return new Range((Index)bytePosStart, (Index)reader.BytesConsumed);
        }
        reader.Read();

        // Read until index
        for (int i = 0; i < index; i++)
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                // Array ended before index. Return the whole array as the location
                return new Range((Index)bytePosStart, (Index)reader.BytesConsumed);
            }

            reader.Skip();
            reader.Read();
        }
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            // Array ended at index. Return the whole array as the location
            return new Range((Index)bytePosStart, (Index)reader.BytesConsumed);
        }

        return GetByteRangeFromJsonPointerSegments(ref reader, childSegments);
    }

    private static Range ReadObject(ref Utf8JsonReader reader, string segment, ReadOnlySpan<string> childSegments)
    {
        Debug.Assert(reader.TokenType == JsonTokenType.StartObject);
        long objectByteStart = reader.TokenStartIndex;
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    Debug.Assert(propertyName is not null);
                    if (propertyName != segment)
                    {
                        // Skip the property if it's not the one we're looking for
                        reader.Read();
                        reader.Skip();
                        break;
                    }
                    if (childSegments.IsEmpty)
                    {
                        // Add location to both property name and value
                        return new Range((Index)reader.TokenStartIndex, (Index)reader.BytesConsumed);
                    }
                    reader.Read();
                    return GetByteRangeFromJsonPointerSegments(ref reader, childSegments);
                case JsonTokenType.EndObject:
                    return new Range((Index)objectByteStart, (Index)reader.BytesConsumed); // Return full parent object, if specific pointer is not found
                default:
                    throw new UnreachableException($"Unexpected token {reader.TokenType} at {reader.BytesConsumed}");
            }
        }
        throw new UnreachableException("Unexpected end of file");
    }
}
